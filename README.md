# java-csharp-migration
WPF migration of Order Entry System from Java Swing to C# 
## How to run
-using the commands on cursor terminal : 
dotnet restore
dotnet build
dotnet run
-or:
Open the project folder in Cursor
Open the OrderEntryApp.csproj file
Press Run or F5

## Requirements
- Windows OS

## Completed
- Customer management (view, add, edit)
- Order management (view, create with line items)
- Product catalog view
- Discount calculation (5%, 10%, 15% tiers)
- Tax calculation (8%)
- Grand total computation
- SQLite database integration



## Assumptions
- SQLite database schema unchanged

## Migration plan
- Analyzed Java code structure in VS Code
- Created project structure in Cursor (UI, DB, Models folders)
- Migrated database layer first (foundation)
- Migrated customer management (simpler feature)
- Migrated order creation (complex feature)
- Testing and bug fixes



## AI usage
- Used AI to translate Java logic to C#
- Manually reviewed and tested queries
