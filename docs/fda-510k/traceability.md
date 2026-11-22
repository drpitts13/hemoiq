# Requirements traceability

| Requirement | Code | Tests |
|---|---|---|
| SRS-IU-01 | `SoftwareIdentity`, `MainWindow.ShowAbout`, first-run, F1, `ReportService.GeneratePreviewText` | `Fda510kControlTests.SoftwareIdentity_*`, `Reports_IncludeIntendedUseVersionAndTraceLine` |
| SRS-IU-02 | `SoftwareIdentity.Version`, About / reports | `SoftwareIdentity_ExposesAssemblyVersionAndIntendedUse` |
| SRS-SEED-01 | `MainViewModel` constructor (no seeder calls) | `FreshDatabase_HasNoSeededSpecimens` |
| SRS-SEED-02 | `DemoDataGuard` (used by `App.xaml.cs --seed-clinical` and `MainWindow.LoadDemoData`) | `DemoDataGuardTests` |
| SRS-AUD-01 / 02 | `DatabaseService.AppendAudit` on confirm/clear/reaction/panel/rule/purge/restore; settings dialog | `ConfirmAndClear_WriteAuditEvents_AndRequireReason`, `SettingsChange_CanBeAudited` |
| SRS-AUD-03 | `AuditLogDialog` | Manual; export uses `GetAuditEvents` |
| SRS-LOCK-01 | `EnsureSpecimenUnlocked`, analyzer `updateDb` guard | `ConfirmedSpecimen_LocksReactionsAndAnalysisWrites` |
| SRS-LOCK-02 | `ClearSpecimenFinalCall(reason)` + `ReasonDialog` | `ConfirmAndClear_WriteAuditEvents_AndRequireReason` |
| SRS-SNAP-01 | `SaveAnalysisSnapshot` / `PersistSnapshot`, `AntibodyAnalyzer.ComputeInputFingerprint` | `AnalyzeSpecimen_PersistsTraceabilitySnapshot`, `InputFingerprintTests` |
| SRS-SNAP-02 | `ReportService.AnalysisTraceLine` | `Reports_IncludeIntendedUseVersionAndTraceLine` |
| SRS-LOG-01 | `AppLog`, `App.OnStartup` | `AppLog_WritesVersionedLineWithoutThrowing`, `AppLogPrivacyTests` |
| SRS-CYB-01 | `Environment.UserName` on audit; `AccessControl.Demand` and `EnsureCanRecord` on every writer; NTFS ACL tests | `HipaaSecurityTests`, `AccessControlTests`, `RecordResultsPermissionTests` |
| SRS-SAFE-01 | `ReactionsViewModel.ExpiredSpecimenPrompt`, `PanelEditActivation.ExpiryWarning`, `ExpiredReagentCheck` | `ExpiredReagentTests`, `ExpiredSpecimenEntryTests`, `PanelEditAuditTests` |
| SRS-SAFE-02 | `DatabaseService.SetEluateSource`, `UpdateSpecimenMrn` | `EluateEvidenceTests` |
| SRS-SAFE-03 | `LabPanelImportService`, `VendorPanelImportService`, `PanelEditActivation`, `PanelsViewModel.ActivatePanel` | `PanelEditAuditTests`, `LabCsvImportTests` |
| SRS-SAFE-04 | `RunTreatmentText`, `ReactionsViewModel.AddRun` | `RunTreatmentTextTests` |
| SRS-SAFE-05 | `PanelCsvService.Import`, `StructuredPanelParser` | `LabCsvImportTests`, `StructuredImportParserTests` |
| SRS-SAFE-06 | `DestructiveConfirm` | `DestructiveConfirmTests` |
| SRS-SAFE-07 | `DatabaseService` constructor, `StartupFailure`, `App.OnStartup`, `RunInTransaction` | `StartupFailureTests` |
| SRS-SAFE-08 | `ReportService.ScoreLabel`, `AntigenConstants.AcsExceptionCriterion` | `AcsEvaluationTests` |
| SRS-SAFE-09 | `OverrideConfirm` | `OverrideConfirmTests` |

Algorithm / clinical performance (pre-existing): `AntibodyAnalysisTests`, `AcsEvaluationTests`, `ScenarioTests`, `ValidationStressTests`, `ReportAccuracyTests`.
