# C# Doc Fold

**Automatically fold C# XML documentation comments to keep your code clean and readable.**

AutoFoldSummaries collapses all `///` XML documentation blocks whenever you open or switch to a C# file, so you can focus on the code that matters. Summaries are still just one click away when you need them.

---
## Features

- **Auto-fold on file open** — XML doc blocks collapse the moment a `.cs` file is opened.
- **Auto-fold on tab switch** — Switching to a C# tab re-folds any expanded doc blocks.
- **Respects manual unfolds** — Unfolding a specific block during editing won't cause other blocks to re-collapse until the next tab switch.

---
## Options

AutoFoldSummaries can be toggled on and off via Tools -> AutoFoldSummaries in Visual Studio.

---
## The use case

C# XML documentation is valuable for IntelliSense and generated docs, but it clutters your editor. A well-documented class can easily double in visual length:

```csharp
/// <summary>
/// Represents a customer account in the system.
/// Handles validation, state transitions, and
/// integration with the billing service.
/// </summary>
/// <remarks>
/// This class is thread-safe for concurrent reads
/// but requires external synchronization for writes.
/// </remarks>
public class CustomerAccount
{
    /// <summary>
    /// Gets or sets the unique identifier for the account.
    /// </summary>
    /// <value>
    /// A <see cref="Guid"/> that uniquely identifies this account.
    /// </value>
    public Guid Id { get; set; }

    /// <summary>
    /// Calculates the total balance across all linked sub-accounts,
    /// applying any pending transactions and adjustments.
    /// </summary>
    /// <param name="includeArchived">
    /// If <c>true</c>, includes balances from archived sub-accounts.
    /// </param>
    /// <returns>
    /// The aggregated balance as a <see cref="decimal"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the account is in a suspended state.
    /// </exception>
    public decimal GetTotalBalance(bool includeArchived = false)
    {
        // ...
    }
}
```
With AutoFoldSummaries, you see this instead:

```csharp
/// <summary> Represents a customer account in the system. ...
public class CustomerAccount
{
    /// <summary> Gets or sets the unique identifier for the account. ...
    public Guid Id { get; set; }

    /// <summary> Calculates the total balance across all linked sub-accounts, ...
    public decimal GetTotalBalance(bool includeArchived = false)
    {
        // ...
    }
}
```
