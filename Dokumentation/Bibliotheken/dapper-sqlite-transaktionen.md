# Dapper mit Microsoft.Data.Sqlite — Transaktionen über mehrere Tabellen

Belegt durch Probe-Test am 2026-08-29 (Dapper 2.1.79, Microsoft.Data.Sqlite 10.0.11, .NET 10) mit Fault-Injection nach Skill `dependency-probe`. Genutzt von R00001 (`BoardRepository.LegeAn`).

## Muster

```csharp
using var verbindung = new SqliteConnection("Data Source=kanbanc.db");
verbindung.Open();
using var transaktion = verbindung.BeginTransaction();

var boardNummer = verbindung.ExecuteScalar<long>(
    "INSERT INTO Board (Name) VALUES (@Name); SELECT last_insert_rowid();",
    new { Name = "Entwicklung" }, transaktion);

verbindung.Execute(
    "INSERT INTO Spalte (BoardNummer, Bezeichnung) VALUES (@BoardNummer, @Bezeichnung)",
    spaltenAlsArray, transaktion);

transaktion.Commit();
```

- `BeginTransaction()` auf der offenen Verbindung; die `IDbTransaction` wird jedem `Execute`/`ExecuteScalar`/`Query` als dritter Parameter mitgegeben.
- Ohne `Commit()` wird beim `Dispose` der Transaktion zurückgerollt — der Fehlerpfad braucht kein eigenes `Rollback()`.
- Die vergebene Nummer liefert `SELECT last_insert_rowid();` **im selben Befehl** wie das `INSERT`, per `ExecuteScalar<long>`. SQLite-Autoincrement-Werte sind `long`, nicht `int`.
- Ein Array anonymer Objekte an `Execute` führt das Statement je Element aus (Dapper-Batch) — für die drei Standardspalten ausreichend.

## Verifiziert

| Fall | Ergebnis |
|---|---|
| Board + 3 Spalten, Commit | Board 1, 3 Spalten vorhanden |
| Board + Spalte mit `NULL` in `NOT NULL`-Spalte | `SqliteException` Code 19, nach Rollback ist **auch das Board weg** — die Transaktion umschließt beide Tabellen |
| Migrationsskript zweimal ausgeführt | kein Fehler (`CREATE TABLE IF NOT EXISTS`) |

## Befund, der von SQL Server abweicht

**`Execute` ohne `transaction`-Parameter innerhalb einer offenen Transaktion wirft bei SQLite keinen Fehler.** Bei `SqlClient` scheitert das laut („ExecuteNonQuery requires the command to have a transaction"); `Microsoft.Data.Sqlite` lässt es durch, und das Statement nimmt trotzdem an der Transaktion teil — SQLite-Transaktionen gelten je Verbindung, nicht je Befehl. Im Probe-Test wurde ein so abgesetztes `INSERT` beim `Rollback` mit zurückgenommen.

Folge: Ein vergessener `transaction`-Parameter ist **fachlich unschädlich, aber unsichtbar**. Der Compiler und die Laufzeit helfen nicht. Deshalb im Repository jeden Aufruf innerhalb einer Transaktion mit dem Parameter schreiben, auch wenn es ohne liefe — sonst bricht das Muster beim ersten Wechsel des Providers.

## Nicht geprüft

- Verhalten bei zwei gleichzeitigen Schreibern (Datei-Lock, `SQLITE_BUSY`). Für den Einzelbetrieb im LAN mit einer WebApi-Instanz nicht relevant; wird es relevant, `busy_timeout` in der Verbindungszeichenfolge prüfen.
- `TransactionScope` — nicht verwendet, weil die explizite `IDbTransaction` im Repository sichtbarer ist (C24, sichtbarer Fehlerpfad).

## Quellen

- Probe: Scratchpad der Sitzung vom 2026-08-29 (`probe/Program.cs`), Ausgabe `PROBE BESTANDEN`
- [Learn Dapper — Transaction](https://www.learndapper.com/misc/transaction)
- [Dave Paquette — Managing Database Transactions in Dapper](https://www.davepaquette.com/archive/2019/02/06/managing-transactions-in-dapper.aspx)
