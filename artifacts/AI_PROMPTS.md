# AI Prompts and Conversations - Order Entry Migration

**Project:** Java Swing to C# WPF Migration  
**Tool Used:** Cursor AI  
**Date:** February 2, 2026  
**Time Allocated:** 90 minutes

1. Project Setup and Structure
### Prompt:
-inserted screenshots of java folder structure
-Create a WPF .NET project structure with separate folders for UI (windows/views), Database access layer, and Model classes for a customer order management system.
### AI Response:
- Created folder structure: UI/, DB/, Models/
- Generated OrderEntryApp.csproj file
- Gave command codes to create them through the cursor terminal

## 2. Customer Model Migration

### Prompt:

Convert this Java Customer class to C#
[I pasted the Java  CustomerWindow , CustomerScreen.java code here, then CustomerDialog.java and each where seperately handled]
Make it compatible with WPF data binding.

### AI Response:
- Generated C# Customer class with auto-properties

## 2. Order Model Migration
Convert this Java Order class to C#
[I pasted the Java OrderWindow , then OrderScreen.java code here, then OrderEditorDialog.java and each where seperately handled]
Make it compatible with WPF data binding.

### AI Response:
- Generated C# Customer class and ported Java discount and tax calculation logic to C# with the calculateDiscount(), calculateTax(), and calculateTotal() methods

## 3. Database Connection Setup

### Prompt:
Translate this Java JDBC SQLite database connection code to C# :
[I pasted the Java DatabaseManager connection code]
I made sure that the database is the same with no additional changes at all
Give the command to connect csharp to sqlite in the cursor terminal

### AI Response:
- Created DatabaseManager.cs class
- Used SQLiteConnection for database access
- Generated connection string

## 4. Domain folder migration:

### Prompt:
insert and convert these files (they become classes that hold data and rules for each concept)
for all: Customer, Order, OrderLine, Product
inserted them seperately and created there .cs files beforehand

## 5. Full Revision:
sent both folder structures and details and double checked for any missing files or info that must be used

## 6. ReadME file:
give me a well structure way of writing the ReadMe dile, then started filling it out

## 7. GitHub:
push my code project to github through assisting me with the sorrect order of cmmands and steps







