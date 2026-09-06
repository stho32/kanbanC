# Blazor Server, `InputFile` und überlappende `change`-Ereignisse

Belegt durch den Diagnoselauf zu [R00024](../../Anforderungen/R00024-stiller-anhangverlust-beim-zweiten-ablegen.md) (35 Beobachtungen) und durch den Behebungslauf am 2026-09-06 (.NET 10, Microsoft.Playwright 1.62.0). Genutzt von `Kartendetail.razor` (`NimmDatei`, `UebernimmAnhang`) und `Ablegeflaechenstand`.

## Ein zweites `change`-Ereignis tötet die laufende Übertragung

Das JavaScript hinter `<InputFile>` hält die gewählten Dateien in einer Zuordnung am Element und **leert sie bei jedem `change`-Ereignis**, bevor es die neue Auswahl einträgt. Eine Übertragung, die noch über `OpenReadStream` liest, sucht danach eine Datei-Nummer, die der Browser nicht mehr kennt.

Folgen, alle im Lauf beobachtet:

- Der Strom stirbt mitten im Lesen; die Bytes erreichen den Server nie, es entsteht **keine** Zeile und **keine** halbe Datei.
- Die Ausnahme ist `Microsoft.JSInterop.JSException` — **keine** `HttpRequestException`. Ein Fangpunkt, der nur letztere kennt, sieht den Abbruch nicht.
- Bleibt sie ungefangen, verlässt sie den Ereignishandler und erscheint im Browser als `pageerror: Error: There was an exception invoking 'NotifyChange'` — ohne die Blazor-Ausnahmeanzeige (`#blazor-error-ui`) und ohne Meldung auf der Seite. Der Verlust ist für den Menschen unsichtbar.
- Der Fangpunkt muss das `await using var inhalt = datei.OpenReadStream(...)` **einschließen**: auch das Verwerfen des Stroms kann werfen, wenn die Referenz schon tot ist.

Die Gegenmaßnahme liegt an der Fläche, nicht dahinter: Wer das zweite Ereignis gar nicht erst entstehen lässt (`disabled` am Feld, solange ein Anhängen läuft), verhindert den Verlust. Hinter dem Ereignis lässt er sich nur noch **melden** — die Datei ist zu diesem Zeitpunkt bereits tot, auch wenn der C#-Handler des zweiten Vorgangs sofort zurückkehrt.

## Der Sperrzustand muss vor dem ersten `await` gezeichnet werden

Ein Ereignishandler zeichnet erst nach seinem Ende neu. Ein Zustand, der die Fläche sperrt, erreicht den Browser deshalb nur dann rechtzeitig, wenn er **vor** dem ersten `await` gesetzt und mit `StateHasChanged()` sofort abgeschickt wird:

```csharp
_laufendeDatei = datei.Name;
StateHasChanged();
try { await HaengeAn(datei); }
catch (JSException) { /* Abbruch melden */ }
finally { _laufendeDatei = null; }
```

Steht das `StateHasChanged()` weiter unten, ist das Feld im entscheidenden Moment noch offen. Das Freigeben gehört ins `finally` — jeder Ausgang (Erfolg, Zurückweisung, Größenwächter, Abbruch) muss die Fläche wieder öffnen.

## Playwright: `SetInputFilesAsync` prüft `enabled` nicht

`ILocator.SetInputFilesAsync` ist die einzige Aktion **ohne** Actionability-Prüfung: der Treiber prüft nur, dass das Ziel ein `input` ist (nach `follow-label`-Retargeting), und setzt die Dateien auch auf ein `disabled`-Feld — samt `input`- und `change`-Ereignis. Blazors Ereignisweiterleitung unterdrückt `change` auf gesperrten Elementen nicht (nur Zeigerereignisse).

Für Tests heißt das:

- Ein Test, der den Bedienweg eines Menschen abbildet, muss **selbst** warten, bis die Fläche frei ist (`Assertions.Expect(feld).ToBeEnabledAsync()`), sonst legt er Dateien auf ein Feld, das kein Mensch bedienen könnte. In `KartendetailSeite.HaengeDateiAn` steht dieser Wartepunkt.
- Umgekehrt ist genau das der Weg, auf dem sich eine Überlappung **erzwingen** lässt, die die Oberfläche nicht anbietet (`KartendetailSeite.ErzwingeAblegen`) — der Prüfstand für die Zusage „kein Ablegevorgang endet stumm".

## Die Größen gehören zur Reproduktion

Mit 1000 und 2000 Bytes ist die erste Übertragung durch, bevor die zweite beginnt: der Fehler blieb in 13 von 13 isolierten Läufen unsichtbar. Mit **3 MB zuerst und 2 kB danach** kippt er zuverlässig. Wer die Größen in einem Reproduktionstest „aufräumt", schaltet ihn ab.
