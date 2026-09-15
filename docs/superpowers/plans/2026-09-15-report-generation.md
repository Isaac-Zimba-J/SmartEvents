# Report Generation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Organizers and company admins can download an event attendee list, an event sales report, and a venue bookings report as PDF or Excel.

**Architecture:** A `ReportsController` queries EF Core, builds plain report records, and hands them to an `IReportRenderer` chosen by `?format=`. Two renderers (`PdfReportRenderer` via QuestPDF, `ExcelReportRenderer` via ClosedXML) live in `Infrastructure/Reports/` and never touch the DbContext. Angular gets a `ReportsService` (blob download) and a reusable `ReportDownloadComponent` placed on the attendees page and venue detail page.

**Tech Stack:** .NET 10 / ASP.NET Core, EF Core, QuestPDF 2026.9.0, ClosedXML 0.105.1, xunit 2.9.3 (new test project), Angular 18 standalone components, Lucide icons.

**Spec:** `docs/superpowers/specs/2026-09-15-report-generation-design.md`

## Global Constraints

- Backend conventions from `CLAUDE.md`: primary-constructor DI, no service layer (renderers follow the `QrCodeService` precedent), error bodies are `new { message = "..." }`, roles via `nameof(UserRole.X)`, all timestamps UTC.
- Frontend conventions: standalone components, `providedIn: 'root'` services, `{ next, error }` observer pattern, `err.error?.message ?? 'Fallback'`, Lucide icons registered in `app.config.ts`, raw SCSS (no Tailwind).
- Currency is ZMW displayed with a `K` prefix (`K #,##0.00`).
- Package versions are pinned: QuestPDF `2026.9.0`, ClosedXML `0.105.1`, xunit `2.9.3`, xunit.runner.visualstudio `4.0.0`, Microsoft.NET.Test.Sdk `18.10.0`.
- QuestPDF licence: `QuestPDF.Settings.License = LicenseType.Community` — set once in `PdfReportRenderer`'s static constructor so both the API and the tests are covered (the spec says `Program.cs`; the static ctor is the same effect with one fewer place to forget).
- Run all commands from the repo root: `/Users/zimbadev/Documents/Workspace/Peoples Projects/SmartEvents`.
- Each task ends with a commit. Existing uncommitted PawaPay/SignalR changes are in the tree — only `git add` the files each task names.

---

### Task 1: Test project + report models

**Files:**
- Create: `SmartEvents.API.Tests/SmartEvents.API.Tests.csproj`
- Create: `SmartEvents.API.Tests/Reports/ReportFixtures.cs`
- Create: `SmartEvents.API/Infrastructure/Reports/ReportModels.cs`
- Modify: `SmartEvents.sln` (via `dotnet sln add`)

**Interfaces:**
- Produces: the records below, used by every later task. `ReportFixtures.Attendees()`, `.Sales()`, `.VenueBookings()` return populated instances for tests.

- [ ] **Step 1: Create the test project and add it to the solution**

```bash
dotnet new xunit -n SmartEvents.API.Tests -o SmartEvents.API.Tests --framework net10.0
dotnet sln SmartEvents.sln add SmartEvents.API.Tests/SmartEvents.API.Tests.csproj
dotnet add SmartEvents.API.Tests reference SmartEvents.API/SmartEvents.API.csproj
rm SmartEvents.API.Tests/UnitTest1.cs
```

Then replace the generated csproj with exactly this (pins versions, adds the ASP.NET framework reference the web project needs):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.10.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="4.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../SmartEvents.API/SmartEvents.API.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Write the report models**

`SmartEvents.API/Infrastructure/Reports/ReportModels.cs`:

```csharp
namespace SmartEvents.API.Infrastructure.Reports;

// Shared by every report. Renderers depend only on the records in this file.
public record ReportHeader(
    string CompanyName,
    string Title,
    string Subject,
    DateTime GeneratedAt,
    string GeneratedBy
);

public record SummaryItem(string Label, string Value);

public record AttendeeRow(
    int Number,
    string Name,
    string Email,
    string Phone,
    string TicketNumber,
    string Status,
    DateTime RegisteredAt,
    DateTime? CheckedInAt
);

public record AttendeeReport(
    ReportHeader Header,
    IReadOnlyList<SummaryItem> Summary,
    IReadOnlyList<AttendeeRow> Rows
);

public record SalesRow(
    DateTime Date,
    string Attendee,
    string TicketNumber,
    string Method,
    decimal Amount,
    string Status,
    string Reference
);

public record SalesReport(
    ReportHeader Header,
    IReadOnlyList<SummaryItem> Summary,
    IReadOnlyList<SalesRow> Rows
);

public record VenueBookingRow(
    string BookedBy,
    string Email,
    DateTime Start,
    DateTime End,
    int Days,
    string Status,
    string PaymentStatus,
    string Method,
    decimal Amount,
    string Reference
);

public record VenueBookingsReport(
    ReportHeader Header,
    IReadOnlyList<SummaryItem> Summary,
    IReadOnlyList<VenueBookingRow> Rows
);
```

- [ ] **Step 3: Write the shared test fixtures**

`SmartEvents.API.Tests/Reports/ReportFixtures.cs`:

```csharp
using SmartEvents.API.Infrastructure.Reports;

namespace SmartEvents.API.Tests.Reports;

public static class ReportFixtures
{
    private static readonly DateTime T = new(2026, 9, 15, 10, 30, 0, DateTimeKind.Utc);

    public static ReportHeader Header(string title) =>
        new("TechEvents Co.", title, "Dev Summit Malawi 2026 · 2026-10-01 · Lilongwe ICC", T, "Olivia Organizer");

    public static AttendeeReport Attendees(int rows = 2) => new(
        Header("Attendee List"),
        [new("Confirmed", "2"), new("Waitlisted", "0"), new("Checked in", "1"), new("Capacity", "100")],
        Enumerable.Range(1, rows).Select(i => new AttendeeRow(
            i, $"Person {i}", $"person{i}@example.com", "260971234567",
            $"SE-20260915-{i:D8}", i == 1 ? "CheckedIn" : "Confirmed",
            T.AddDays(-i), i == 1 ? T : null)).ToList());

    public static SalesReport Sales(int rows = 2) => new(
        Header("Sales Report"),
        [new("Tickets sold", "2"), new("Gross revenue", "K 100.00"), new("Airtel Money", "1 · K 50.00"),
         new("MTN MoMo", "1 · K 50.00"), new("Pending", "0"), new("Failed", "0")],
        Enumerable.Range(1, rows).Select(i => new SalesRow(
            T.AddHours(-i), $"Person {i}", $"SE-20260915-{i:D8}",
            i % 2 == 0 ? "MTN MoMo" : "Airtel Money", 50m, "Completed", $"ref-{i}")).ToList());

    public static VenueBookingsReport VenueBookings(int rows = 2) => new(
        new ReportHeader("TechEvents Co.", "Venue Bookings", "Lilongwe ICC — Area 3, Lilongwe · Capacity 500 · K 2,000/day", T, "Alice Admin"),
        [new("Total bookings", "2"), new("Confirmed", "2"), new("Days booked", "3"), new("Revenue", "K 6,000.00")],
        Enumerable.Range(1, rows).Select(i => new VenueBookingRow(
            $"Person {i}", $"person{i}@example.com", T.AddDays(i), T.AddDays(i + 1), i,
            "Confirmed", "Completed", "Airtel Money", 2000m * i, $"VB-{i:D6}")).ToList());
}
```

- [ ] **Step 4: Build to verify everything compiles**

Run: `dotnet build SmartEvents.sln -v q --nologo`
Expected: `Build succeeded.` (pre-existing warnings about MailKit / SignalR are fine).

- [ ] **Step 5: Commit**

```bash
git add SmartEvents.sln SmartEvents.API.Tests SmartEvents.API/Infrastructure/Reports/ReportModels.cs
git commit -m "feat(reports): report models and test project scaffold

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 2: `IReportRenderer` + `PdfReportRenderer`

**Files:**
- Create: `SmartEvents.API/Infrastructure/Reports/IReportRenderer.cs`
- Create: `SmartEvents.API/Infrastructure/Reports/PdfReportRenderer.cs`
- Create: `SmartEvents.API.Tests/Reports/PdfReportRendererTests.cs`
- Modify: `SmartEvents.API/SmartEvents.API.csproj` (add QuestPDF)

**Interfaces:**
- Consumes: records from Task 1.
- Produces: `IReportRenderer { string Format; string ContentType; string FileExtension; byte[] Render(AttendeeReport); byte[] Render(SalesReport); byte[] Render(VenueBookingsReport); }` and `PdfReportRenderer : IReportRenderer` with `Format == "pdf"`.

- [ ] **Step 1: Add the package**

```bash
dotnet add SmartEvents.API package QuestPDF --version 2026.9.0
```

- [ ] **Step 2: Write the failing tests**

`SmartEvents.API.Tests/Reports/PdfReportRendererTests.cs`:

```csharp
using System.Text;
using SmartEvents.API.Infrastructure.Reports;

namespace SmartEvents.API.Tests.Reports;

public class PdfReportRendererTests
{
    private readonly PdfReportRenderer _sut = new();

    [Fact]
    public void Describes_itself_as_pdf()
    {
        Assert.Equal("pdf", _sut.Format);
        Assert.Equal("application/pdf", _sut.ContentType);
        Assert.Equal("pdf", _sut.FileExtension);
    }

    [Fact]
    public void Renders_attendee_report_as_pdf_bytes()
    {
        var bytes = _sut.Render(ReportFixtures.Attendees());
        AssertIsPdf(bytes);
    }

    [Fact]
    public void Renders_sales_report_as_pdf_bytes()
    {
        var bytes = _sut.Render(ReportFixtures.Sales());
        AssertIsPdf(bytes);
    }

    [Fact]
    public void Renders_venue_bookings_report_as_pdf_bytes()
    {
        var bytes = _sut.Render(ReportFixtures.VenueBookings());
        AssertIsPdf(bytes);
    }

    [Fact]
    public void Renders_report_with_no_rows()
    {
        var bytes = _sut.Render(ReportFixtures.Attendees(rows: 0));
        AssertIsPdf(bytes);
    }

    [Fact]
    public void Renders_many_rows_across_pages()
    {
        // 120 rows will not fit on one landscape A4 page; QuestPDF must paginate, not throw.
        var bytes = _sut.Render(ReportFixtures.Attendees(rows: 120));
        AssertIsPdf(bytes);
        Assert.True(bytes.Length > 10_000);
    }

    private static void AssertIsPdf(byte[] bytes)
    {
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 4);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test SmartEvents.API.Tests -v q --nologo`
Expected: build error — `PdfReportRenderer` does not exist.

- [ ] **Step 4: Write the interface**

`SmartEvents.API/Infrastructure/Reports/IReportRenderer.cs`:

```csharp
namespace SmartEvents.API.Infrastructure.Reports;

public interface IReportRenderer
{
    /// <summary>Value accepted by the ?format= query parameter, e.g. "pdf".</summary>
    string Format { get; }
    string ContentType { get; }
    string FileExtension { get; }

    byte[] Render(AttendeeReport report);
    byte[] Render(SalesReport report);
    byte[] Render(VenueBookingsReport report);
}
```

- [ ] **Step 5: Write the PDF renderer**

`SmartEvents.API/Infrastructure/Reports/PdfReportRenderer.cs`:

```csharp
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SmartEvents.API.Infrastructure.Reports;

public class PdfReportRenderer : IReportRenderer
{
    static PdfReportRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string Format => "pdf";
    public string ContentType => "application/pdf";
    public string FileExtension => "pdf";

    private const string DateTimeFmt = "yyyy-MM-dd HH:mm";
    private const string DateFmt = "yyyy-MM-dd";

    public byte[] Render(AttendeeReport report) => Build(
        report.Header,
        report.Summary,
        columns: ["#", "Name", "Email", "Phone", "Ticket #", "Status", "Registered (UTC)", "Checked in"],
        widths: [0.4f, 1.6f, 2.2f, 1.2f, 1.7f, 1f, 1.3f, 0.9f],
        rightAligned: [0],
        rows: report.Rows.Select(r => new[]
        {
            r.Number.ToString(), r.Name, r.Email, r.Phone, r.TicketNumber, r.Status,
            r.RegisteredAt.ToString(DateTimeFmt), r.CheckedInAt?.ToString("HH:mm") ?? "—"
        }));

    public byte[] Render(SalesReport report) => Build(
        report.Header,
        report.Summary,
        columns: ["Date (UTC)", "Attendee", "Ticket #", "Method", "Amount", "Status", "Reference"],
        widths: [1.3f, 1.8f, 1.7f, 1.2f, 1f, 1f, 2.4f],
        rightAligned: [4],
        rows: report.Rows.Select(r => new[]
        {
            r.Date.ToString(DateTimeFmt), r.Attendee, r.TicketNumber, r.Method,
            Money(r.Amount), r.Status, r.Reference
        }));

    public byte[] Render(VenueBookingsReport report) => Build(
        report.Header,
        report.Summary,
        columns: ["Booked by", "Email", "Start", "End", "Days", "Status", "Payment", "Method", "Amount", "Ref"],
        widths: [1.5f, 2f, 1f, 1f, 0.6f, 1f, 1f, 1.1f, 1f, 1.4f],
        rightAligned: [4, 8],
        rows: report.Rows.Select(r => new[]
        {
            r.BookedBy, r.Email, r.Start.ToString(DateFmt), r.End.ToString(DateFmt), r.Days.ToString(),
            r.Status, r.PaymentStatus, r.Method, Money(r.Amount), r.Reference
        }));

    private static string Money(decimal amount) => $"K {amount:N2}";

    private static byte[] Build(
        ReportHeader header,
        IReadOnlyList<SummaryItem> summary,
        string[] columns,
        float[] widths,
        int[] rightAligned,
        IEnumerable<string[]> rows)
    {
        var document = Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(9));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text(header.CompanyName).FontSize(11).SemiBold().FontColor(Colors.Grey.Darken2);
                        left.Item().Text(header.Title).FontSize(18).Bold();
                        left.Item().Text(header.Subject).FontSize(10);
                    });
                    row.ConstantItem(220).AlignRight().Column(right =>
                    {
                        right.Item().AlignRight().Text($"Generated {header.GeneratedAt:yyyy-MM-dd HH:mm} UTC")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                        right.Item().AlignRight().Text($"by {header.GeneratedBy}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });

                col.Item().PaddingTop(10).Row(row =>
                {
                    foreach (var item in summary)
                    {
                        row.RelativeItem().PaddingRight(6).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(tile =>
                        {
                            tile.Item().Text(item.Label).FontSize(8).FontColor(Colors.Grey.Darken1);
                            tile.Item().Text(item.Value).FontSize(12).SemiBold();
                        });
                    }
                });

                col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });

            page.Content().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    foreach (var w in widths) cols.RelativeColumn(w);
                });

                table.Header(h =>
                {
                    for (var i = 0; i < columns.Length; i++)
                    {
                        var cell = h.Cell().Background(Colors.Grey.Lighten3).Padding(4);
                        if (rightAligned.Contains(i)) cell.AlignRight().Text(columns[i]).SemiBold();
                        else cell.Text(columns[i]).SemiBold();
                    }
                });

                var rowIndex = 0;
                foreach (var row in rows)
                {
                    var background = rowIndex++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                    for (var i = 0; i < row.Length; i++)
                    {
                        var cell = table.Cell()
                            .Background(background)
                            .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(4);
                        if (rightAligned.Contains(i)) cell.AlignRight().Text(row[i]);
                        else cell.Text(row[i]);
                    }
                }

                if (rowIndex == 0)
                {
                    table.Cell().ColumnSpan((uint)columns.Length).Padding(14).AlignCenter()
                        .Text("No data").FontColor(Colors.Grey.Darken1);
                }
            });

            page.Footer().Row(row =>
            {
                row.RelativeItem().Text("Generated by SmartEvents").FontSize(8).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        }));

        return document.GeneratePdf();
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test SmartEvents.API.Tests -v q --nologo`
Expected: `Passed! - Failed: 0, Passed: 6`.

If QuestPDF throws a font-related exception, do not guess at font names — QuestPDF bundles Lato and should need no system fonts. Read the exception text and report it.

- [ ] **Step 7: Commit**

```bash
git add SmartEvents.API/SmartEvents.API.csproj SmartEvents.API/Infrastructure/Reports/IReportRenderer.cs SmartEvents.API/Infrastructure/Reports/PdfReportRenderer.cs SmartEvents.API.Tests/Reports/PdfReportRendererTests.cs
git commit -m "feat(reports): IReportRenderer and QuestPDF renderer

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 3: `ExcelReportRenderer`

**Files:**
- Create: `SmartEvents.API/Infrastructure/Reports/ExcelReportRenderer.cs`
- Create: `SmartEvents.API.Tests/Reports/ExcelReportRendererTests.cs`
- Modify: `SmartEvents.API/SmartEvents.API.csproj` (add ClosedXML)

**Interfaces:**
- Consumes: `IReportRenderer` and records from Tasks 1–2.
- Produces: `ExcelReportRenderer : IReportRenderer` with `Format == "xlsx"`. Workbook layout: sheet `Summary`, then a data sheet named `Attendees` / `Sales` / `Bookings`.

- [ ] **Step 1: Add the package**

```bash
dotnet add SmartEvents.API package ClosedXML --version 0.105.1
```

- [ ] **Step 2: Write the failing tests**

`SmartEvents.API.Tests/Reports/ExcelReportRendererTests.cs`:

```csharp
using ClosedXML.Excel;
using SmartEvents.API.Infrastructure.Reports;

namespace SmartEvents.API.Tests.Reports;

public class ExcelReportRendererTests
{
    private readonly ExcelReportRenderer _sut = new();

    [Fact]
    public void Describes_itself_as_xlsx()
    {
        Assert.Equal("xlsx", _sut.Format);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", _sut.ContentType);
        Assert.Equal("xlsx", _sut.FileExtension);
    }

    [Fact]
    public void Output_starts_with_zip_signature()
    {
        var bytes = _sut.Render(ReportFixtures.Attendees());
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
    }

    [Fact]
    public void Attendee_workbook_has_summary_and_data_sheets()
    {
        using var wb = Open(_sut.Render(ReportFixtures.Attendees(rows: 3)));

        Assert.Equal(["Summary", "Attendees"], wb.Worksheets.Select(w => w.Name).ToArray());

        var summary = wb.Worksheet("Summary");
        Assert.Equal("Attendee List", summary.Cell(1, 1).GetString());
        Assert.Equal("Confirmed", summary.Cell(6, 1).GetString());
        Assert.Equal("2", summary.Cell(6, 2).GetString());

        var data = wb.Worksheet("Attendees");
        Assert.Equal("#", data.Cell(1, 1).GetString());
        Assert.Equal("Checked in (UTC)", data.Cell(1, 8).GetString());
        Assert.True(data.Row(1).Style.Font.Bold);
        Assert.Equal(1, data.SheetView.SplitRow);            // header row frozen
        Assert.Equal(4, data.LastRowUsed()!.RowNumber());     // header + 3 rows
        Assert.Equal("Person 1", data.Cell(2, 2).GetString());
        Assert.Equal("—", data.Cell(3, 8).GetString());       // null CheckedInAt renders as a dash
    }

    [Fact]
    public void Sales_workbook_formats_amounts_as_kwacha_and_dates()
    {
        using var wb = Open(_sut.Render(ReportFixtures.Sales(rows: 2)));

        var data = wb.Worksheet("Sales");
        Assert.Equal("Amount", data.Cell(1, 5).GetString());
        Assert.Equal(50m, data.Cell(2, 5).GetValue<decimal>());
        Assert.Equal("\"K \"#,##0.00", data.Cell(2, 5).Style.NumberFormat.Format);
        Assert.Equal("yyyy-mm-dd hh:mm", data.Cell(2, 1).Style.NumberFormat.Format);
        Assert.Equal(new DateTime(2026, 9, 15, 9, 30, 0), data.Cell(2, 1).GetDateTime());
    }

    [Fact]
    public void Venue_bookings_workbook_has_bookings_sheet_with_ten_columns()
    {
        using var wb = Open(_sut.Render(ReportFixtures.VenueBookings(rows: 1)));

        var data = wb.Worksheet("Bookings");
        Assert.Equal(10, data.LastColumnUsed()!.ColumnNumber());
        Assert.Equal("Ref", data.Cell(1, 10).GetString());
        Assert.Equal(2000m, data.Cell(2, 9).GetValue<decimal>());
    }

    [Fact]
    public void Renders_report_with_no_rows()
    {
        using var wb = Open(_sut.Render(ReportFixtures.Sales(rows: 0)));
        var data = wb.Worksheet("Sales");
        Assert.Equal(1, data.LastRowUsed()!.RowNumber());     // header only
    }

    private static XLWorkbook Open(byte[] bytes) => new(new MemoryStream(bytes));
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test SmartEvents.API.Tests -v q --nologo`
Expected: build error — `ExcelReportRenderer` does not exist.

- [ ] **Step 4: Write the Excel renderer**

`SmartEvents.API/Infrastructure/Reports/ExcelReportRenderer.cs`:

```csharp
using ClosedXML.Excel;

namespace SmartEvents.API.Infrastructure.Reports;

public class ExcelReportRenderer : IReportRenderer
{
    public string Format => "xlsx";
    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public string FileExtension => "xlsx";

    private const string MoneyFormat = "\"K \"#,##0.00";
    private const string DateTimeFormat = "yyyy-mm-dd hh:mm";
    private const string DateFormat = "yyyy-mm-dd";

    // Wrapper so the row builder can tell "date only" from "date + time".
    private readonly record struct DateOnlyCell(DateTime Value);

    public byte[] Render(AttendeeReport report) => Build(
        report.Header,
        report.Summary,
        sheetName: "Attendees",
        columns: ["#", "Name", "Email", "Phone", "Ticket #", "Status", "Registered (UTC)", "Checked in (UTC)"],
        rows: report.Rows.Select(r => new object?[]
        {
            r.Number, r.Name, r.Email, r.Phone, r.TicketNumber, r.Status, r.RegisteredAt, r.CheckedInAt
        }));

    public byte[] Render(SalesReport report) => Build(
        report.Header,
        report.Summary,
        sheetName: "Sales",
        columns: ["Date (UTC)", "Attendee", "Ticket #", "Method", "Amount", "Status", "Reference"],
        rows: report.Rows.Select(r => new object?[]
        {
            r.Date, r.Attendee, r.TicketNumber, r.Method, r.Amount, r.Status, r.Reference
        }));

    public byte[] Render(VenueBookingsReport report) => Build(
        report.Header,
        report.Summary,
        sheetName: "Bookings",
        columns: ["Booked by", "Email", "Start", "End", "Days", "Status", "Payment", "Method", "Amount", "Ref"],
        rows: report.Rows.Select(r => new object?[]
        {
            r.BookedBy, r.Email, new DateOnlyCell(r.Start), new DateOnlyCell(r.End), r.Days,
            r.Status, r.PaymentStatus, r.Method, r.Amount, r.Reference
        }));

    private static byte[] Build(
        ReportHeader header,
        IReadOnlyList<SummaryItem> summary,
        string sheetName,
        string[] columns,
        IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();

        var summarySheet = workbook.AddWorksheet("Summary");
        summarySheet.Cell(1, 1).Value = header.Title;
        summarySheet.Cell(1, 1).Style.Font.Bold = true;
        summarySheet.Cell(1, 1).Style.Font.FontSize = 14;
        summarySheet.Cell(2, 1).Value = header.CompanyName;
        summarySheet.Cell(3, 1).Value = header.Subject;
        summarySheet.Cell(4, 1).Value = $"Generated {header.GeneratedAt:yyyy-MM-dd HH:mm} UTC by {header.GeneratedBy}";

        var summaryRow = 6;
        foreach (var item in summary)
        {
            summarySheet.Cell(summaryRow, 1).Value = item.Label;
            summarySheet.Cell(summaryRow, 1).Style.Font.Bold = true;
            summarySheet.Cell(summaryRow, 2).Value = item.Value;
            summaryRow++;
        }
        summarySheet.Columns().AdjustToContents();

        var dataSheet = workbook.AddWorksheet(sheetName);
        for (var i = 0; i < columns.Length; i++)
            dataSheet.Cell(1, i + 1).Value = columns[i];
        dataSheet.Row(1).Style.Font.Bold = true;
        dataSheet.SheetView.FreezeRows(1);

        var rowNumber = 2;
        foreach (var row in rows)
        {
            for (var i = 0; i < row.Length; i++)
                WriteCell(dataSheet.Cell(rowNumber, i + 1), row[i]);
            rowNumber++;
        }
        dataSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteCell(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Value = "—";
                break;
            case decimal amount:
                cell.Value = amount;
                cell.Style.NumberFormat.Format = MoneyFormat;
                break;
            case DateOnlyCell d:
                cell.Value = d.Value;
                cell.Style.NumberFormat.Format = DateFormat;
                break;
            case DateTime dt:
                cell.Value = dt;
                cell.Style.NumberFormat.Format = DateTimeFormat;
                break;
            case int n:
                cell.Value = n;
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test SmartEvents.API.Tests -v q --nologo`
Expected: `Passed! - Failed: 0, Passed: 12`.

- [ ] **Step 6: Commit**

```bash
git add SmartEvents.API/SmartEvents.API.csproj SmartEvents.API/Infrastructure/Reports/ExcelReportRenderer.cs SmartEvents.API.Tests/Reports/ExcelReportRendererTests.cs
git commit -m "feat(reports): ClosedXML Excel renderer

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 4: `ReportsController` + DI registration

**Files:**
- Create: `SmartEvents.API/Controllers/ReportsController.cs`
- Modify: `SmartEvents.API/Program.cs` — DI block after `AddSingleton<IQrCodeService, QrCodeService>()` (line ~73) and the CORS policy (lines ~21-30)

**Interfaces:**
- Consumes: `IReportRenderer` implementations (Tasks 2–3), `SmartEventsDbContext`, `SlugHelper.Generate`.
- Produces: `GET /api/reports/events/{eventId}/attendees`, `GET /api/reports/events/{eventId}/sales`, `GET /api/reports/venues/{venueId}/bookings`, each with `?format=pdf|xlsx` (default `pdf`), returning a file with a `Content-Disposition` the browser is allowed to read.

There is no integration-test harness (the API needs Postgres), so this task is verified with `curl` against the running API in Step 4.

- [ ] **Step 1: Register renderers and expose the download header**

In `SmartEvents.API/Program.cs`, change the CORS policy so the frontend can read the filename:

```csharp
    options.AddPolicy("SmartEventsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("Content-Disposition");
    });
```

Directly under `builder.Services.AddSingleton<IQrCodeService, QrCodeService>();` add:

```csharp
builder.Services.AddSingleton<IReportRenderer, PdfReportRenderer>();
builder.Services.AddSingleton<IReportRenderer, ExcelReportRenderer>();
```

and add `using SmartEvents.API.Infrastructure.Reports;` to the usings at the top.

- [ ] **Step 2: Write the controller**

`SmartEvents.API/Controllers/ReportsController.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvents.API.Application.Helpers;
using SmartEvents.API.Domain.Entities;
using SmartEvents.API.Domain.Enums;
using SmartEvents.API.Infrastructure.Data;
using SmartEvents.API.Infrastructure.Reports;

namespace SmartEvents.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController(SmartEventsDbContext db, IEnumerable<IReportRenderer> renderers) : ControllerBase
{
    [HttpGet("events/{eventId:guid}/attendees")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)},{nameof(UserRole.Organizer)}")]
    public async Task<IActionResult> EventAttendees(Guid eventId, [FromQuery] string format = "pdf")
    {
        var renderer = ResolveRenderer(format);
        if (renderer is null) return BadRequest(new { message = "Unsupported format. Use pdf or xlsx." });

        var user = await CurrentUserAsync();
        if (user is null) return Unauthorized();

        var ev = await db.Events
            .Include(e => e.Company)
            .Include(e => e.Venue)
            .Include(e => e.Registrations).ThenInclude(r => r.User)
            .Include(e => e.Registrations).ThenInclude(r => r.Ticket)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev is null) return NotFound(new { message = "Event not found." });
        if (!CanAccessEvent(ev, user)) return Forbid();

        var rows = ev.Registrations
            .Where(r => r.Status != RegistrationStatus.Cancelled)
            .OrderBy(r => r.User.LastName).ThenBy(r => r.User.FirstName)
            .Select((r, i) => new AttendeeRow(
                i + 1,
                $"{r.User.FirstName} {r.User.LastName}",
                r.User.Email,
                r.User.Phone ?? "—",
                r.Ticket?.TicketNumber ?? "—",
                r.Status.ToString(),
                r.RegisteredAt,
                r.CheckedInAt))
            .ToList();

        var confirmed = ev.Registrations.Count(r => r.Status is RegistrationStatus.Confirmed or RegistrationStatus.CheckedIn);
        var waitlisted = ev.Registrations.Count(r => r.Status == RegistrationStatus.Waitlisted);
        var checkedIn = ev.Registrations.Count(r => r.Status == RegistrationStatus.CheckedIn || r.CheckedInAt != null);

        var report = new AttendeeReport(
            BuildHeader(ev.Company.Name, "Attendee List", EventSubject(ev), user),
            [
                new("Confirmed", confirmed.ToString()),
                new("Waitlisted", waitlisted.ToString()),
                new("Checked in", checkedIn.ToString()),
                new("Capacity", ev.MaxAttendees.ToString())
            ],
            rows);

        return File(renderer.Render(report), renderer.ContentType, FileName("attendees", ev.Slug, renderer));
    }

    [HttpGet("events/{eventId:guid}/sales")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)},{nameof(UserRole.Organizer)}")]
    public async Task<IActionResult> EventSales(Guid eventId, [FromQuery] string format = "pdf")
    {
        var renderer = ResolveRenderer(format);
        if (renderer is null) return BadRequest(new { message = "Unsupported format. Use pdf or xlsx." });

        var user = await CurrentUserAsync();
        if (user is null) return Unauthorized();

        var ev = await db.Events
            .Include(e => e.Company)
            .Include(e => e.Venue)
            .Include(e => e.Registrations).ThenInclude(r => r.User)
            .Include(e => e.Registrations).ThenInclude(r => r.Ticket)
            .Include(e => e.Registrations).ThenInclude(r => r.Payment)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev is null) return NotFound(new { message = "Event not found." });
        if (!CanAccessEvent(ev, user)) return Forbid();

        var payments = ev.Registrations
            .Where(r => r.Payment is not null)
            .Select(r => (Registration: r, Payment: r.Payment!))
            .OrderByDescending(x => x.Payment.CreatedAt)
            .ToList();

        var rows = payments.Select(x => new SalesRow(
                x.Payment.CreatedAt,
                $"{x.Registration.User.FirstName} {x.Registration.User.LastName}",
                x.Registration.Ticket?.TicketNumber ?? "—",
                MethodLabel(x.Payment.Method),
                x.Payment.Amount,
                x.Payment.Status.ToString(),
                x.Payment.TransactionReference ?? "—"))
            .ToList();

        var completed = payments.Where(x => x.Payment.Status == PaymentStatus.Completed).Select(x => x.Payment).ToList();
        var airtel = completed.Where(p => p.Method == PaymentMethod.AirtelMoney).ToList();
        var mtn = completed.Where(p => p.Method == PaymentMethod.MTNMoMo).ToList();

        var report = new SalesReport(
            BuildHeader(ev.Company.Name, "Sales Report", EventSubject(ev), user),
            [
                new("Tickets sold", completed.Count.ToString()),
                new("Gross revenue", Money(completed.Sum(p => p.Amount))),
                new("Airtel Money", $"{airtel.Count} · {Money(airtel.Sum(p => p.Amount))}"),
                new("MTN MoMo", $"{mtn.Count} · {Money(mtn.Sum(p => p.Amount))}"),
                new("Pending", payments.Count(x => x.Payment.Status == PaymentStatus.Pending).ToString()),
                new("Failed", payments.Count(x => x.Payment.Status == PaymentStatus.Failed).ToString())
            ],
            rows);

        return File(renderer.Render(report), renderer.ContentType, FileName("sales", ev.Slug, renderer));
    }

    [HttpGet("venues/{venueId:guid}/bookings")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
    public async Task<IActionResult> VenueBookings(Guid venueId, [FromQuery] string format = "pdf")
    {
        var renderer = ResolveRenderer(format);
        if (renderer is null) return BadRequest(new { message = "Unsupported format. Use pdf or xlsx." });

        var user = await CurrentUserAsync();
        if (user is null) return Unauthorized();

        var venue = await db.Venues
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == venueId);

        if (venue is null) return NotFound(new { message = "Venue not found." });
        if (!CanAccessVenue(venue, user)) return Forbid();

        var bookings = await db.VenueBookings
            .Include(b => b.User)
            .Where(b => b.VenueId == venueId)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync();

        var rows = bookings.Select(b => new VenueBookingRow(
                $"{b.User.FirstName} {b.User.LastName}",
                b.User.Email,
                b.StartDate,
                b.EndDate,
                DaysBooked(b),
                b.Status.ToString(),
                b.PaymentStatus.ToString(),
                MethodLabel(b.PaymentMethod),
                b.TotalAmount,
                b.TransactionRef ?? "—"))
            .ToList();

        var confirmedBookings = bookings.Where(b => b.Status == VenueBookingStatus.Confirmed).ToList();
        var price = venue.PricePerDay is { } p ? $"K {p:N0}/day" : "price on request";
        var subject = $"{venue.Name} — {venue.Address}, {venue.City} · Capacity {venue.Capacity:N0} · {price}";

        var report = new VenueBookingsReport(
            BuildHeader(venue.Company.Name, "Venue Bookings", subject, user),
            [
                new("Total bookings", bookings.Count.ToString()),
                new("Confirmed", confirmedBookings.Count.ToString()),
                new("Days booked", confirmedBookings.Sum(DaysBooked).ToString()),
                new("Revenue", Money(bookings.Where(b => b.PaymentStatus == PaymentStatus.Completed).Sum(b => b.TotalAmount)))
            ],
            rows);

        return File(renderer.Render(report), renderer.ContentType, FileName("venue-bookings", SlugHelper.Generate(venue.Name), renderer));
    }

    // ---

    private IReportRenderer? ResolveRenderer(string format) =>
        renderers.FirstOrDefault(r => r.Format.Equals(format, StringComparison.OrdinalIgnoreCase));

    private async Task<User?> CurrentUserAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await db.Users.FindAsync(userId);
    }

    private static bool CanAccessEvent(Event ev, User user) => user.Role switch
    {
        UserRole.SuperAdmin => true,
        UserRole.CompanyAdmin => user.CompanyId == ev.CompanyId,
        UserRole.Organizer => ev.OrganizerId == user.Id,
        _ => false
    };

    private static bool CanAccessVenue(Venue venue, User user) => user.Role switch
    {
        UserRole.SuperAdmin => true,
        UserRole.CompanyAdmin => user.CompanyId == venue.CompanyId,
        _ => false
    };

    private static ReportHeader BuildHeader(string companyName, string title, string subject, User user) =>
        new(companyName, title, subject, DateTime.UtcNow, $"{user.FirstName} {user.LastName}");

    private static string EventSubject(Event ev)
    {
        var venue = ev.Venue?.Name ?? ev.VenueText ?? "Venue TBA";
        return $"{ev.Title} · {ev.StartDate:yyyy-MM-dd} · {venue}";
    }

    private static string FileName(string report, string slug, IReportRenderer renderer) =>
        $"{report}-{slug}-{DateTime.UtcNow:yyyyMMdd}.{renderer.FileExtension}";

    private static string Money(decimal amount) => $"K {amount:N2}";

    private static string MethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.AirtelMoney => "Airtel Money",
        PaymentMethod.MTNMoMo => "MTN MoMo",
        PaymentMethod.Free => "Free",
        _ => method.ToString()
    };

    // Same rule VenueBookingsController uses to price a booking.
    private static int DaysBooked(VenueBooking b) => Math.Max(1, (b.EndDate.Date - b.StartDate.Date).Days);
}
```

- [ ] **Step 3: Build**

Run: `dotnet build SmartEvents.sln -v q --nologo`
Expected: `Build succeeded.` with no new warnings.

- [ ] **Step 4: Verify against the running API**

Postgres must be running (`pg_isready -h localhost -p 5432`). Start the API in the background and wait for it:

```bash
nohup dotnet run --project SmartEvents.API/SmartEvents.API.csproj --urls http://localhost:5148 > /tmp/se-reports-api.log 2>&1 &
until curl -s -o /dev/null --max-time 2 -w "%{http_code}" http://localhost:5148/api/events | grep -q 200; do sleep 1; done; echo ready
```

Then run this verification script (save to the scratchpad directory, not the repo):

```bash
#!/bin/zsh
set -u
API=http://localhost:5148/api
login() { curl -s -X POST $API/auth/login -H 'Content-Type: application/json' -d "{\"email\":\"$1\",\"password\":\"Seed1234!\"}" | python3 -c 'import sys,json;print(json.load(sys.stdin)["accessToken"])'; }
ORG=$(login organizer@techevents.co)
ADM=$(login admin@techevents.co)
ATT=$(login attendee@example.com)

# an event the organizer owns, and a venue in their company
EV=$(curl -s $API/events/managed -H "Authorization: Bearer $ORG" | python3 -c 'import sys,json;e=json.load(sys.stdin);print(e[0]["id"])')
VEN=$(curl -s "$API/venues" -H "Authorization: Bearer $ADM" | python3 -c 'import sys,json;d=json.load(sys.stdin);v=d["items"] if isinstance(d,dict) else d;print(v[0]["id"])')
OUT=$(mktemp -d)

check() { # label token url expectedCode expectedMagic
  code=$(curl -s -o "$OUT/$1" -w '%{http_code}' -D "$OUT/$1.h" -H "Authorization: Bearer $2" "$3")
  magic=$(head -c 4 "$OUT/$1" | tr -d '\0')
  disp=$(grep -i '^content-disposition' "$OUT/$1.h" | tr -d '\r' | sed 's/.*filename=//; s/;.*//')
  printf '%-34s HTTP %s  magic=%-5s file=%s\n' "$1" "$code" "$magic" "$disp"
  [ "$code" = "$4" ] || echo "   ✗ expected HTTP $4"
  [ -z "$5" ] || [[ "$magic" == "$5"* ]] || echo "   ✗ expected magic $5"
}

check attendees.pdf   $ORG "$API/reports/events/$EV/attendees?format=pdf"  200 '%PDF'
check attendees.xlsx  $ORG "$API/reports/events/$EV/attendees?format=xlsx" 200 'PK'
check sales.pdf       $ORG "$API/reports/events/$EV/sales"                 200 '%PDF'
check sales.xlsx      $ORG "$API/reports/events/$EV/sales?format=xlsx"     200 'PK'
check bookings.pdf    $ADM "$API/reports/venues/$VEN/bookings?format=pdf"  200 '%PDF'
check bookings.xlsx   $ADM "$API/reports/venues/$VEN/bookings?format=xlsx" 200 'PK'
check bad-format      $ORG "$API/reports/events/$EV/attendees?format=csv"  400 ''
check attendee-denied $ATT "$API/reports/events/$EV/attendees"             403 ''
check org-no-venues   $ORG "$API/reports/venues/$VEN/bookings"             403 ''
check missing-event   $ORG "$API/reports/events/00000000-0000-0000-0000-000000000000/attendees" 404 ''
echo "files in $OUT"
```

Expected: every line shows the expected HTTP code and magic, no `✗` lines. Then open `$OUT/attendees.pdf` and `$OUT/sales.pdf` with the Read tool and confirm: company name + title + subject in the header, summary tiles, a striped table, `Page 1 / 1` in the footer.

Stop the API afterwards: `pkill -f "SmartEvents.API"`.

- [ ] **Step 5: Commit**

```bash
git add SmartEvents.API/Controllers/ReportsController.cs SmartEvents.API/Program.cs
git commit -m "feat(reports): attendees, sales and venue bookings report endpoints

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 5: Angular `ReportsService` + `ReportDownloadComponent`

**Files:**
- Create: `SmartEvents.UI/src/app/core/models/report.models.ts`
- Create: `SmartEvents.UI/src/app/core/services/reports.service.ts`
- Create: `SmartEvents.UI/src/app/core/components/report-download/report-download.component.ts`
- Create: `SmartEvents.UI/src/app/core/components/report-download/report-download.component.html`
- Create: `SmartEvents.UI/src/app/core/components/report-download/report-download.component.scss`
- Modify: `SmartEvents.UI/src/app/app.config.ts` (icon imports + `pick`)

**Interfaces:**
- Consumes: the three API endpoints from Task 4; `ToastService.error(message)`; `environment.apiUrl`.
- Produces: `ReportFormat`, `ReportsService.downloadEventAttendees(eventId, format)`, `.downloadEventSales(eventId, format)`, `.downloadVenueBookings(venueId, format)` (each `Observable<void>`), and `<app-report-download [label] [download]>` where `download: (format: ReportFormat) => Observable<void>`.

- [ ] **Step 1: Model**

`SmartEvents.UI/src/app/core/models/report.models.ts`:

```ts
export type ReportFormat = 'pdf' | 'xlsx';
```

- [ ] **Step 2: Service**

`SmartEvents.UI/src/app/core/services/reports.service.ts`:

```ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Observable, from, throwError } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ReportFormat } from '../models/report.models';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly apiUrl = `${environment.apiUrl}/reports`;

  constructor(private http: HttpClient) {}

  downloadEventAttendees(eventId: string, format: ReportFormat): Observable<void> {
    return this.download(`${this.apiUrl}/events/${eventId}/attendees`, format);
  }

  downloadEventSales(eventId: string, format: ReportFormat): Observable<void> {
    return this.download(`${this.apiUrl}/events/${eventId}/sales`, format);
  }

  downloadVenueBookings(venueId: string, format: ReportFormat): Observable<void> {
    return this.download(`${this.apiUrl}/venues/${venueId}/bookings`, format);
  }

  private download(url: string, format: ReportFormat): Observable<void> {
    return this.http
      .get(url, { params: { format }, responseType: 'blob', observe: 'response' })
      .pipe(
        map(res => this.saveBlob(res, format)),
        catchError(err => this.normalizeBlobError(err))
      );
  }

  private saveBlob(res: HttpResponse<Blob>, format: ReportFormat): void {
    // ASP.NET sends: attachment; filename=x.pdf; filename*=UTF-8''x.pdf
    const disposition = res.headers.get('Content-Disposition') ?? '';
    const match = /filename="?([^";]+)"?/i.exec(disposition);
    const fileName = match?.[1] ?? `report.${format}`;

    const objectUrl = URL.createObjectURL(res.body!);
    const anchor = document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(objectUrl);
  }

  // Error bodies arrive as a Blob because we asked for one; turn them back into { message }
  // so callers can keep using err.error?.message like everywhere else in the app.
  private normalizeBlobError(err: HttpErrorResponse): Observable<never> {
    if (!(err.error instanceof Blob)) return throwError(() => err);

    return from(err.error.text()).pipe(
      switchMap(text => {
        let message = 'Report download failed.';
        try { message = JSON.parse(text)?.message ?? message; } catch { /* not JSON */ }
        return throwError(() => new HttpErrorResponse({
          error: { message },
          status: err.status,
          statusText: err.statusText,
          url: err.url ?? undefined
        }));
      })
    );
  }
}
```

- [ ] **Step 3: Component**

`report-download.component.ts`:

```ts
import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { Observable } from 'rxjs';
import { ReportFormat } from '../../models/report.models';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-report-download',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './report-download.component.html',
  styleUrls: ['./report-download.component.scss']
})
export class ReportDownloadComponent {
  @Input({ required: true }) label = '';
  @Input({ required: true }) download!: (format: ReportFormat) => Observable<void>;

  busy: ReportFormat | null = null;

  constructor(private toast: ToastService) {}

  run(format: ReportFormat): void {
    if (this.busy) return;
    this.busy = format;
    this.download(format).subscribe({
      next: () => { this.busy = null; },
      error: err => {
        this.toast.error(err.error?.message ?? 'Report download failed.');
        this.busy = null;
      }
    });
  }
}
```

`report-download.component.html`:

```html
<div class="report-download">
  <span class="report-label">{{ label }}</span>
  <button type="button" class="btn-secondary btn-sm" (click)="run('pdf')" [disabled]="busy !== null">
    <lucide-angular name="file-text" [size]="14"></lucide-angular>
    {{ busy === 'pdf' ? 'Preparing…' : 'PDF' }}
  </button>
  <button type="button" class="btn-secondary btn-sm" (click)="run('xlsx')" [disabled]="busy !== null">
    <lucide-angular name="file-spreadsheet" [size]="14"></lucide-angular>
    {{ busy === 'xlsx' ? 'Preparing…' : 'Excel' }}
  </button>
</div>
```

`report-download.component.scss`:

```scss
.report-download {
  display: inline-flex;
  align-items: center;
  gap: .5rem;
}

.report-label {
  font-size: .85rem;
  font-weight: 600;
  opacity: .8;
}

button {
  display: inline-flex;
  align-items: center;
  gap: .3rem;
}
```

- [ ] **Step 4: Register the icons**

In `SmartEvents.UI/src/app/app.config.ts`, add `FileText, FileSpreadsheet, Download` to the `lucide-angular` import list and to `LucideAngularModule.pick({...})`:

```ts
import {
  LucideAngularModule,
  Eye, EyeOff,
  LayoutDashboard, Calendar, MapPin, Building2, Users, BarChart2, User, LogOut,
  Plus, Pencil, Trash2, ArrowLeft, ArrowRight, Ticket, QrCode,
  Check, X, Shield, Mail, Phone, Globe, Clock,
  Search, ChevronRight, Settings, AlertCircle, CheckCircle,
  ScanLine, Star, Tag, DollarSign, Briefcase, BookMarked, Bell, Sparkles,
  FileText, FileSpreadsheet, Download
} from 'lucide-angular';
```

```ts
    importProvidersFrom(LucideAngularModule.pick({
      Eye, EyeOff,
      LayoutDashboard, Calendar, MapPin, Building2, Users, BarChart2, User, LogOut,
      Plus, Pencil, Trash2, ArrowLeft, ArrowRight, Ticket, QrCode,
      Check, X, Shield, Mail, Phone, Globe, Clock,
      Search, ChevronRight, Settings, AlertCircle, CheckCircle,
      ScanLine, Star, Tag, DollarSign, Briefcase, BookMarked, Bell, Sparkles,
      FileText, FileSpreadsheet, Download
    }))
```

- [ ] **Step 5: Build the frontend**

Run: `cd SmartEvents.UI && npx ng build --configuration development 2>&1 | tail -15`
Expected: `Application bundle generation complete.` with no errors (the component is not yet used anywhere, so it is tree-shaken — that is fine; this step only proves it compiles).

- [ ] **Step 6: Commit**

```bash
git add SmartEvents.UI/src/app/core/models/report.models.ts SmartEvents.UI/src/app/core/services/reports.service.ts SmartEvents.UI/src/app/core/components/report-download SmartEvents.UI/src/app/app.config.ts
git commit -m "feat(reports): ReportsService and ReportDownloadComponent

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 6: Place the buttons — attendees page and venue detail

**Files:**
- Modify: `SmartEvents.UI/src/app/features/organizer/attendees/attendees.component.ts`
- Modify: `SmartEvents.UI/src/app/features/organizer/attendees/attendees.component.html:1-6`
- Modify: `SmartEvents.UI/src/app/features/venues/venue-detail/venue-detail.component.ts`
- Modify: `SmartEvents.UI/src/app/features/venues/venue-detail/venue-detail.component.html:69-71`
- Modify: `SmartEvents.UI/src/styles.scss` (one small rule)

**Interfaces:**
- Consumes: `ReportsService`, `ReportDownloadComponent`, `ReportFormat` from Task 5; `EventsService.getManaged()` (returns `EventSummary[]` with `isTicketed`); `AuthService.isAdmin()` (SuperAdmin or CompanyAdmin).

- [ ] **Step 1: Attendees page — component**

Replace `attendees.component.ts` with:

```ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { RegistrationsService } from '../../../core/services/registrations.service';
import { EventsService } from '../../../core/services/events.service';
import { ReportsService } from '../../../core/services/reports.service';
import { ToastService } from '../../../core/services/toast.service';
import { RegistrationResponse } from '../../../core/models/registration.models';
import { EventSummary } from '../../../core/models/event.models';
import { ReportFormat } from '../../../core/models/report.models';
import { ReportDownloadComponent } from '../../../core/components/report-download/report-download.component';

@Component({
  selector: 'app-attendees',
  standalone: true,
  imports: [CommonModule, RouterLink, ReportDownloadComponent],
  templateUrl: './attendees.component.html'
})
export class AttendeesComponent implements OnInit {
  registrations: RegistrationResponse[] = [];
  event: EventSummary | null = null;
  loading = true;
  eventId = '';

  constructor(
    private route: ActivatedRoute,
    private registrationsService: RegistrationsService,
    private eventsService: EventsService,
    private reportsService: ReportsService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.eventId = this.route.snapshot.paramMap.get('eventId')!;
    this.registrationsService.getByEvent(this.eventId).subscribe({
      next: data => { this.registrations = data; this.loading = false; },
      error: () => {
        this.toast.error('Failed to load attendees.');
        this.loading = false;
      }
    });
    // Only needed to know whether the event is ticketed (sales report button).
    this.eventsService.getManaged().subscribe({
      next: events => { this.event = events.find(e => e.id === this.eventId) ?? null; },
      error: () => {}
    });
  }

  // Arrow properties so `this` survives being passed as an input.
  downloadAttendees = (format: ReportFormat) => this.reportsService.downloadEventAttendees(this.eventId, format);
  downloadSales = (format: ReportFormat) => this.reportsService.downloadEventSales(this.eventId, format);

  get showSalesReport(): boolean { return this.event === null || this.event.isTicketed; }

  get confirmed(): RegistrationResponse[] { return this.registrations.filter(r => r.status === 'Confirmed'); }
  get checkedIn(): RegistrationResponse[] { return this.registrations.filter(r => r.status === 'CheckedIn'); }
  get waitlisted(): RegistrationResponse[] { return this.registrations.filter(r => r.status === 'Waitlisted'); }
  get cancelled(): RegistrationResponse[] { return this.registrations.filter(r => r.status === 'Cancelled'); }
}
```

- [ ] **Step 2: Attendees page — template**

Replace the `page-header` block (lines 1-6 of `attendees.component.html`) with:

```html
<div class="attendees-page">
  <div class="page-header">
    <a routerLink="/organizer">&larr; Organizer Dashboard</a>
    <h1>Attendees</h1>
    <a routerLink="/registrations/checkin" class="btn-secondary">Check-In Scanner</a>
  </div>

  <div class="report-actions">
    <app-report-download label="Attendee list" [download]="downloadAttendees"></app-report-download>
    @if (showSalesReport) {
      <app-report-download label="Sales report" [download]="downloadSales"></app-report-download>
    }
  </div>
```

(Everything from `@if (loading)` onward stays as it is.)

- [ ] **Step 3: Venue detail — component**

In `venue-detail.component.ts`:

Add imports:

```ts
import { ReportsService } from '../../../core/services/reports.service';
import { ReportFormat } from '../../../core/models/report.models';
import { ReportDownloadComponent } from '../../../core/components/report-download/report-download.component';
```

Change the decorator's `imports` to:

```ts
  imports: [CommonModule, FormsModule, RouterLink, LucideAngularModule, ReportDownloadComponent],
```

Add `private reportsService: ReportsService` as the last constructor parameter:

```ts
  constructor(
    private route: ActivatedRoute,
    private venuesService: VenuesService,
    private venueBookingsService: VenueBookingsService,
    public authService: AuthService,
    private toast: ToastService,
    private reportsService: ReportsService
  ) {}
```

Add this property directly after the `paymentMethods` array:

```ts
  downloadBookings = (format: ReportFormat) =>
    this.reportsService.downloadVenueBookings(this.venue!.id, format);
```

- [ ] **Step 4: Venue detail — template**

In `venue-detail.component.html`, replace

```html
      <hr class="section-divider" />

      <!-- Booking Section -->
```

with

```html
      @if (authService.isAdmin()) {
        <div class="report-actions">
          <app-report-download label="Bookings report" [download]="downloadBookings"></app-report-download>
        </div>
      }

      <hr class="section-divider" />

      <!-- Booking Section -->
```

- [ ] **Step 5: Shared layout rule**

Append to `SmartEvents.UI/src/styles.scss`:

```scss
.report-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 1.25rem;
  margin: .75rem 0 1.25rem;
}
```

- [ ] **Step 6: Build and click-test**

Run: `cd SmartEvents.UI && npx ng build --configuration development 2>&1 | tail -15`
Expected: no errors.

Then, with the API running (see Task 4 Step 4) and `npx ng serve`:
1. Log in as `organizer@techevents.co`, open Organizer Dashboard → an event → Attendees. Both *Attendee list* and *Sales report* button groups appear; clicking PDF downloads `attendees-<slug>-<date>.pdf`, clicking Excel downloads the `.xlsx`. The button shows "Preparing…" while in flight.
2. Log in as `admin@techevents.co`, open a venue. *Bookings report* appears above "Book This Venue"; downloads work.
3. Log in as `attendee@example.com`, open the same venue: no *Bookings report* group.

Stop the API afterwards: `pkill -f "SmartEvents.API"`.

- [ ] **Step 7: Commit**

```bash
git add SmartEvents.UI/src/app/features/organizer/attendees SmartEvents.UI/src/app/features/venues/venue-detail SmartEvents.UI/src/styles.scss
git commit -m "feat(reports): download buttons on attendees and venue detail pages

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 7: CLAUDE.md

**Files:**
- Modify: `CLAUDE.md` — Solution Structure tree, Key Conventions list, Change Log table.

- [ ] **Step 1: Solution structure**

Under `│       ├── Notifications/    # EmailService, SmsService, NotificationDispatcher, EmailTemplates` add:

```
│       ├── Reports/          # ReportModels, IReportRenderer, PdfReportRenderer (QuestPDF), ExcelReportRenderer (ClosedXML)
```

and after the `SmartEvents.Shared/` block add:

```
├── SmartEvents.API.Tests/    # xunit — renderer tests only (the API itself needs Postgres; verify endpoints with curl)
│   └── Reports/
```

- [ ] **Step 2: Convention**

Add as item 9 in **Key Conventions to Follow**:

```
9. **Reports** are generated server-side. `ReportsController` queries EF and builds a record from `Infrastructure/Reports/ReportModels.cs`; an `IReportRenderer` (resolved by `?format=pdf|xlsx`) turns it into bytes. To add a report: add a record + row record to `ReportModels.cs`, add a `Render(...)` overload to `IReportRenderer` and both renderers (they share a private `Build` helper — pass columns and rows, don't duplicate layout), add the endpoint, add a test in `SmartEvents.API.Tests/Reports/`. Never render inside a controller. The CORS policy exposes `Content-Disposition` so the Angular `ReportsService` can name the downloaded file.
```

- [ ] **Step 3: Change log row**

```
| 2026-09-15 | Report generation: GET /api/reports/events/{id}/attendees, /events/{id}/sales, /venues/{id}/bookings, each `?format=pdf\|xlsx`. New Infrastructure/Reports/ (models, IReportRenderer, QuestPDF + ClosedXML renderers, registered as singletons). New SmartEvents.API.Tests xunit project (renderer tests). Frontend: ReportsService (blob download, reads Content-Disposition), ReportDownloadComponent (PDF/Excel buttons), placed on organizer attendees page (attendee list + sales; sales hidden for free events) and venue detail (bookings, admins only). Icons file-text, file-spreadsheet, download. CORS now exposes Content-Disposition. |
```

- [ ] **Step 4: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: report generation in CLAUDE.md

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```
