# HMS Coding Standards

## Naming Conventions
- **Classes:** PascalCase (e.g., PatientService)
- **Methods:** PascalCase (e.g., GetPatientById)
- **Variables:** camelCase (e.g., patientName)
- **Constants:** UPPER_CASE (e.g., MAX_RETRY_COUNT)
- **Private fields:** _camelCase (e.g., _patientRepository)

## File Organization
- One class per file
- Files named after the class they contain
- Group related classes in appropriate folders

## MVVM Guidelines
- Views should not contain business logic
- Use data binding, avoid code-behind
- ViewModels should not reference Views directly
- Use RelayCommand for command binding