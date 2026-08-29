# IOSP — Integration Operation Segregation Principle

## Grundprinzip

IOSP wurde von **Ralf Westphal** und **Stefan Lieser** im Rahmen von **Flow Design** und **Clean Code Development** entwickelt. Die Kernregel lautet:

> **Eine Methode enthaelt entweder nur Logik ODER sie ruft nur andere Methoden auf — niemals beides.**

Daraus ergeben sich exakt zwei Kategorien:

### Operations (Blatt-Methoden)

- Enthalten **nur Logik**: Berechnungen, Transformationen, Kontrollstrukturen, Validierungen
- Rufen **keine** eigenen Methoden auf (Framework/Standard-Library-Aufrufe sind erlaubt)
- Sind die "Blaetter" im Aufrufbaum
- **Leicht testbar** — keine Abhaengigkeiten, keine Mocks noetig

### Integrations (Orchestrierungs-Methoden)

- Enthalten **keine eigene Logik** — nur Aufrufe anderer Methoden
- Orchestrieren den Ablauf: rufen Operations und andere Integrations auf
- Lesen sich wie ein Inhaltsverzeichnis: *was* passiert, nicht *wie*
- Ein minimales `if` zur Ablaufsteuerung ist pragmatisch akzeptabel

### Hybrids (Verstoesse)

- Mischen Logik und Methodenaufrufe — **verletzt IOSP**
- Schwer testbar, schwer lesbar, schwer wartbar
- Muessen durch Refactoring in Operations + Integration aufgeteilt werden

## Visuelle Darstellung

```
┌──────────────────────────────────────────────┐
│              Integration                      │
│  (nur Aufrufe, keine Logik)                  │
│                                              │
│    ┌──────────┐  ┌──────────┐  ┌──────────┐ │
│    │Operation │  │Operation │  │Operation │ │
│    │(Logik)   │  │(Logik)   │  │(Logik)   │ │
│    └──────────┘  └──────────┘  └──────────┘ │
└──────────────────────────────────────────────┘

Integration liest sich wie Rezept:
  1. Daten laden
  2. Validieren
  3. Transformieren
  4. Speichern

Jeder Schritt ist eine Operation mit testbarer Logik.
```

## Code-Beispiele

### OPERATION: Reine Logik

```csharp
// Source/MyApp.BL/Operations/PricingOperations.cs
public static class PricingOperations
{
    // OPERATION: nur Logik, keine Aufrufe eigener Methoden
    public static decimal CalculateDiscount(decimal price, int quantity)
    {
        return quantity switch
        {
            >= 100 => price * 0.15m,
            >= 50 => price * 0.10m,
            >= 10 => price * 0.05m,
            _ => 0m
        };
    }

    // OPERATION: Transformation
    public static OrderSummary CreateSummary(
        List<OrderItem> items, decimal discount, decimal taxRate)
    {
        var subtotal = items.Sum(i => i.Price * i.Quantity);
        var discountedTotal = subtotal - discount;
        var tax = discountedTotal * taxRate;

        return new OrderSummary
        {
            Subtotal = subtotal,
            Discount = discount,
            Tax = tax,
            Total = discountedTotal + tax,
            ItemCount = items.Sum(i => i.Quantity)
        };
    }

    // OPERATION: Validierung
    public static Result<OrderItem> ValidateOrderItem(string name, decimal price, int quantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<OrderItem>.Failure("Artikelname darf nicht leer sein");

        if (price <= 0)
            return Result<OrderItem>.Failure("Preis muss positiv sein");

        if (quantity <= 0)
            return Result<OrderItem>.Failure("Menge muss positiv sein");

        return Result<OrderItem>.Success(new OrderItem(name, price, quantity));
    }
}
```

### INTEGRATION: Nur Orchestrierung

```csharp
// Source/MyApp.BL/Integrations/OrderIntegration.cs
public class OrderIntegration
{
    private readonly IOrderRepository _repository;
    private readonly ITaxService _taxService;

    public OrderIntegration(IOrderRepository repository, ITaxService taxService)
    {
        _repository = repository;
        _taxService = taxService;
    }

    // INTEGRATION: nur Aufrufe, keine Logik
    public async Task<Result<OrderSummary>> ProcessOrderAsync(int orderId)
    {
        var items = await _repository.GetItemsAsync(orderId);
        var totalQuantity = items.Sum(i => i.Quantity);
        var totalPrice = items.Sum(i => i.Price * i.Quantity);

        var discount = PricingOperations.CalculateDiscount(totalPrice, totalQuantity);
        var taxRate = await _taxService.GetCurrentRateAsync();
        var summary = PricingOperations.CreateSummary(items, discount, taxRate);

        await _repository.SaveSummaryAsync(orderId, summary);

        return Result<OrderSummary>.Success(summary);
    }
}
```

### HYBRID — Verstoesst gegen IOSP

```csharp
// SCHLECHT: Mischt Logik und Aufrufe
public async Task<OrderSummary> ProcessOrderAsync(int orderId)
{
    var items = await _repository.GetItemsAsync(orderId);  // Aufruf

    // Logik direkt eingebettet — IOSP-Verstoss!
    decimal discount = 0;
    var total = items.Sum(i => i.Price * i.Quantity);
    if (total > 1000) discount = total * 0.15m;
    else if (total > 500) discount = total * 0.10m;

    await _repository.SaveSummaryAsync(orderId, summary);  // Aufruf
    return summary;
}
```

### Refactoring: Hybrid → IOSP-konform

**Schritt 1:** Logik in Operation extrahieren:
```csharp
// OPERATION
public static decimal CalculateDiscount(decimal total)
{
    if (total > 1000) return total * 0.15m;
    if (total > 500) return total * 0.10m;
    return 0;
}
```

**Schritt 2:** Methode wird zur reinen Integration:
```csharp
// INTEGRATION
public async Task<OrderSummary> ProcessOrderAsync(int orderId)
{
    var items = await _repository.GetItemsAsync(orderId);
    var total = items.Sum(i => i.Price * i.Quantity);
    var discount = PricingOperations.CalculateDiscount(total);
    var summary = PricingOperations.CreateSummary(items, discount, taxRate);
    await _repository.SaveSummaryAsync(orderId, summary);
    return summary;
}
```

## IOSP in Blazor-Komponenten

Auch Razor-Komponenten folgen IOSP: Event-Handler sind **Integrations**, die an Services delegieren.

```csharp
// Source/MyApp.Web/Components/Pages/Orders.razor
@code {
    // INTEGRATION: Event-Handler delegiert, enthaelt keine Logik
    private async Task HandlePlaceOrder()
    {
        _isProcessing = true;
        var result = await OrderService.ProcessOrderAsync(_orderId);

        if (result.IsSuccess)
        {
            _summary = result.Value;
            _errorMessage = null;
        }
        else
        {
            _errorMessage = result.Error;
        }

        _isProcessing = false;
    }
}
```

Das `if/else` hier ist akzeptabel — es ist **Ablaufsteuerung** (welcher UI-Zustand wird gesetzt), nicht **Geschaeftslogik** (wie wird berechnet).

## IOSP in der Verzeichnisstruktur

```
Source/MyApp.BL/
├── Operations/          ← Reine Logik, statische Methoden, keine DI
│   ├── PricingOperations.cs
│   ├── ValidationOperations.cs
│   └── TransformOperations.cs
├── Integrations/        ← Orchestrierung, DI fuer Repositories/Services
│   ├── OrderIntegration.cs
│   └── UserIntegration.cs
├── Models/              ← Datenklassen (weder Operation noch Integration)
│   ├── OrderItem.cs
│   └── OrderSummary.cs
└── Interfaces/          ← Abstrakte Abhaengigkeiten fuer Integrations
    ├── IOrderRepository.cs
    └── ITaxService.cs
```

## Testbarkeit durch IOSP

### Operations testen: Keine Mocks noetig

```csharp
[TestFixture]
public class PricingOperationsTests
{
    [TestCase(100, 0.05)]   // 10-49: 5%
    [TestCase(50, 0.10)]    // 50-99: 10%
    [TestCase(200, 0.15)]   // 100+: 15%
    public void CalculateDiscount_VariousQuantities_ReturnsCorrectRate(
        int quantity, decimal expectedRate)
    {
        var price = 1000m;

        var discount = PricingOperations.CalculateDiscount(price, quantity);

        Assert.That(discount, Is.EqualTo(price * expectedRate));
    }

    [Test]
    public void ValidateOrderItem_EmptyName_ReturnsFailure()
    {
        var result = PricingOperations.ValidateOrderItem("", 10m, 1);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("Artikelname"));
    }
}
```

### Integrations testen: Mocks fuer Abhaengigkeiten

```csharp
[TestFixture]
public class OrderIntegrationTests
{
    private OrderIntegration _sut;
    private IOrderRepository _repository;
    private ITaxService _taxService;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IOrderRepository>();
        _taxService = Substitute.For<ITaxService>();
        _sut = new OrderIntegration(_repository, _taxService);
    }

    [Test]
    public async Task ProcessOrderAsync_ValidOrder_SavesSummary()
    {
        var items = new List<OrderItem>
        {
            new("Widget", 10m, 100)
        };
        _repository.GetItemsAsync(1).Returns(items);
        _taxService.GetCurrentRateAsync().Returns(0.19m);

        var result = await _sut.ProcessOrderAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        await _repository.Received(1).SaveSummaryAsync(1, Arg.Any<OrderSummary>());
    }
}
```

## Vorteile von IOSP

1. **Reduzierte Methodengroesse:** Integrations selten >15 Zeilen, Operations selten >30 Zeilen
2. **Lesbarkeit:** Integrations lesen sich wie ein Ablaufplan
3. **Testbarkeit:** Operations brauchen keine Mocks — reine Input/Output-Tests
4. **Weniger Abhaengigkeiten:** Operations haengen von nichts ab, Integrations nur von Interfaces
5. **Einfacheres Debugging:** Fehler sind entweder in der Logik (Operation) oder in der Orchestrierung (Integration)

## Pragmatische Ausnahmen

- Ein einfaches `if` in einer Integration zur Ablaufsteuerung ist akzeptabel
- LINQ-Ausdruecke wie `.Sum()`, `.Where()` in Integrations sind akzeptabel (Framework-Aufrufe)
- String-Interpolation und einfache Zuweisungen in Integrations sind akzeptabel
- Ziel ist Testbarkeit und Lesbarkeit, nicht dogmatische Reinheit

## Quellen

- [Ralf Westphal: IOSP](https://ralfwestphal.substack.com/p/integration-operation-segregation)
- [Clean Code Developer: Flow Design](https://ccd-akademie.de/en/flow-design/)
- [Frank Kruse: Clean Code with IOSP](https://frank.woopec.net/2024/04/18/hexafour-10-clean-code.html)
