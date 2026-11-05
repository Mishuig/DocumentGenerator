# Document Generator

Windows desktop application for importing DOCX templates, filling `{{placeholders}}`, and generating new Word documents. Microsoft Word is optional; it is used only if the user chooses to open a DOCX for manual editing/viewing.

## Current build status

Version 0.2.0 — expanded MVP source. The project has not yet been compiled or smoke-tested on Windows in this environment. Do not treat this source archive as a verified production binary.

### Implemented
- WPF desktop interface (Ukrainian UI).
- Portable data layout beside the executable: `data`, `templates`, `exports`, `backups`, `logs`.
- Local SQLite catalog of templates.
- DOCX import with package validation.
- Search by template name/category.
- Template metadata editing (name/category).
- Safe library deletion: source file is moved into `backups` before its catalog record is deleted.
- Manual full backup of template files and SQLite database.
- Placeholder discovery in document body, tables, headers, footers, footnotes and endnotes.
- Placeholder replacement even when Word splits marker text across multiple runs.
- Non-breaking spaces for configured full-name fields and between `№` and following digits.
- Generated documents are created from a copy; imported source templates are not edited.
- Empty-field warning, output to `exports`, and optional opening with the default Windows application.
- Error logging to `logs/application.log`.

### Known limitations / next milestones
- The built-in template editor is not yet a Word-like rich-text editor. Use Word or another DOCX editor to lay out templates; the application manages, inspects and fills them.
- Advanced Word structures (text boxes, drawing objects, fields, tracked changes, content controls, nested tables and unusual run structures) need compatibility testing against real documents.
- No conditional/repeating sections, PDF preview/export, cloud synchronization, encryption, user roles or audit trail yet.
- Formatting rules such as non-breaking full names, number prefixes, dates and paragraph keep-with-next still need a dedicated rule engine and UI.

## Requirements
- Windows 10 (version 1809 or later) / Windows 11 x64.
- .NET 10 SDK to build from source. The published portable build is self-contained and does not require the .NET runtime installed.
- Microsoft Word is not required for generation.

## Run from source
```powershell
dotnet restore
dotnet run
```

## Publish portable Windows x64
Run on Windows (recommended) from the project folder:
```powershell
dotnet publish .\DocumentGenerator.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\publish\win-x64
```

Copy the entire `publish\win-x64` folder to a writable location (for example, `C:\Apps\DocumentGenerator`) and run `DocumentGenerator.exe`. Keep the adjacent data folders with the application when moving it to another PC. Do not place the portable folder in `C:\Program Files` unless write permissions are configured; the app intentionally keeps data beside the EXE.

## Placeholder syntax
```text
ДОГОВІР № {{contract_number}}

Я, {{full_name}}, паспорт {{passport_number}},
проживаю за адресою: {{address}}.
```

Variable names support Latin letters, digits, `_`, `-`, and `.`.

## Important
This is an in-progress application. Before using it for legally significant documents, test every template and generated output in Microsoft Word or LibreOffice, compare pagination, paragraph layout, tables, headers/footers, and verify all personal and financial details. Keep backups of original templates.

For a one-click build on Windows, double-click `build-portable.cmd`. It invokes the PowerShell publish script and places the self-contained application in `publish\win-x64`.


## Автоматична збірка через GitHub Actions

Інструкція українською: див. `README-GITHUB-BUILD.md`. Workflow `.github/workflows/build-windows.yml` збирає self-contained Windows x64 ZIP на GitHub Actions; локальний .NET SDK не потрібен.
