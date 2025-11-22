#!/usr/bin/env dotnet fsi

open System
open System.IO

// This script calls the migration service directly without needing the web server

printfn "Starting migration..."

// Note: You can run this with: dotnet fsi migrate-balance-sheets.fsx
printfn "To migrate balance sheets, please:"
printfn "1. Start the backend: cd LoanAnnuityCalculatorAPI && dotnet run"
printfn "2. Open your browser to: http://localhost:5206/api/debtor/4/migrate-balance-sheet"
printfn "3. Or use curl: curl -X POST http://localhost:5206/api/debtor/4/migrate-balance-sheet"
printfn ""
printfn "Alternative: Use the frontend UI once we implement the button!"
