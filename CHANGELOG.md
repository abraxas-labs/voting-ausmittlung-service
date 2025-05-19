# ✨ Changelog (`v2.63.12`)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Info

```text
This version -------- v2.63.12
Previous version ---- v2.51.0
Initial version ----- v1.29.14
Total commits ------- 895
```

## [v2.63.12] - 2025-05-17

### 🔄 Changed

- fix subtotals mapping in secondary election detail protocols

## [v2.63.11] - 2025-05-15

### 🔄 Changed

- fix mandate algorithm mapping in secondary election detail protocols

## [v2.63.10] - 2025-05-15

### 🔄 Changed

- include invalid votes and order secondary majority election ballots in bundle review

## [v2.63.9] - 2025-05-14

### 🔄 Changed

- fix voter participation rounding in protocols

## [v2.63.8] - 2025-05-14

### 🔄 Changed

- fix voter participation rounding in protocols

## [v2.63.7] - 2025-05-14

### 🆕 Added

- add secondary election detail protocols

## [v2.63.6] - 2025-05-14

### 🔄 Changed

- ignore hide lower domain of influence in report when same tenant in detail protocols

## [v2.63.5] - 2025-05-06

### 🔄 Changed

- support partial end results in vote end result protocol

## [v2.63.4] - 2025-05-01

### 🔄 Changed

- improve eCH-0252 write in candidate values

## [v2.63.3] - 2025-04-16

### 🔄 Changed

- correctly report eCH-0252 counting circle types

## [v2.63.2] - 2025-04-16

### 🔄 Changed

- correct calculation of voter participation with hide lower dois flag enabled in protocols

## [v2.63.1] - 2025-04-15

### 🆕 Added

- add counting circle to end result protocols where needed

## [v2.63.0] - 2025-04-15

### 🔄 Changed

- secondary election candidate end result state dependent of primary election result

## [v2.62.8] - 2025-04-15

### 🔄 Changed

- change column order for wabstic proportional election exports

## [v2.62.7] - 2025-04-10

### 🆕 Added

- add candidate title to eCH-0252 exports

## [v2.62.6] - 2025-04-09

### 🔄 Changed

- improve list contests api endpoint performance

## [v2.62.5] - 2025-04-07

### 🔄 Changed

- update pdf diff verified pdfs

## [v2.62.4] - 2025-04-01

### 🔄 Changed

- adjust proportional election wabstic exports

## [v2.62.3] - 2025-03-31

### 🔄 Changed

- correct counting circle validation in e-voting import

## [v2.62.2] - 2025-03-27

### 🔄 Changed

- don't export majority election write in candidates in eCH-0252 information export

## [v2.62.1] - 2025-03-27

### 🆕 Added

- add empty proportional list in eCH-0252 information export

## [v2.62.0] - 2025-03-26

### 🆕 Added

- add counting circle details and ballot results to result overview

## [v2.61.6] - 2025-03-25

### 🔄 Changed

- publish results for multiple submission finished

## [v2.61.5] - 2025-03-21

### 🆕 Added

- add empty vote count disabled flag to majority election protocols

## [v2.61.4] - 2025-03-19

### 🔄 Changed

- esult overview should contain both partial results and owned political businesses

## [v2.61.3] - 2025-03-19

### 🔄 Changed

- eCH-0252 always create XML even when no results are present

## [v2.61.2] - 2025-03-18

### 🔄 Changed

- export api list protocol for accessible political businesses

### 🆕 Added

- add pdf diff project

## [v2.61.1] - 2025-03-17

### 🔄 Changed

- fix(VOTING-5562): fix import processor for legacy events

## [v2.61.0] - 2025-03-17

### 🆕 Added

- add country, street and house number to election candidate

## [v2.60.4] - 2025-03-17

### 🔄 Changed

- fix protocol export event processing

## [v2.60.3] - 2025-03-14

### 🔄 Changed

- remove incorrect contests in contest list summary

## [v2.60.2] - 2025-03-13

### 🔄 Changed

- show contest in monitoring overview as manager without any political business to manage

## [v2.60.1] - 2025-03-13

### 🆕 Added

- add incumbent to proportional election candidates csv exports

## [v2.60.0] - 2025-03-12

### 🆕 Added

- added report level name

## [v2.59.0] - 2025-03-11

### 🆕 Added

- generic event watching

## [v2.58.0] - 2025-03-07

### 🔄 Changed

- generate vote end result report per domain of influence

## [v2.57.2] - 2025-03-06

### 🔄 Changed

- fix processing when missing voting cards get added and sub totals get removed

## [v2.57.1] - 2025-03-05

### 🆕 Added

- eCH-0252 proportional election list union added

## [v2.57.0] - 2025-03-05

### 🆕 Added

- e-counting write-in handling

## [v2.56.1] - 2025-03-04

### 🔄 Changed

- ensure valid majority election ballot

## [v2.56.0] - 2025-02-28

### 🔄 Changed

- e-counting import

## [v2.55.3] - 2025-02-26

### 🔄 Changed

- contest owners see all political businesses in eCH-0252 exports

## [v2.55.2] - 2025-02-26

### 🔄 Changed

- added additional vote infos to eCH-0252 tie break questions

## [v2.55.1] - 2025-02-26

### 🔄 Changed

- reverted routing changes in ExportController and ResultExportController

## [v2.55.0] - 2025-02-24

### 🔄 Changed

- routing of ExportController and ResultExportController. Rename of ResultExportController to ProtocolExportControler

## [v2.54.2] - 2025-02-21

### 🔄 Changed

- update basis majority election ballot groups event processing

## [v2.54.1] - 2025-02-14

### 🔄 Changed

- fix proportional election union end result election count sync
- fix election end result update with super apportionment lot decisions

## [v2.54.0] - 2025-02-13

### 🔄 Changed

- fix subtotals mapping in secondary election detail protocols

### 🔄 Changed

- fix mandate algorithm mapping in secondary election detail protocols

### 🔄 Changed

- include invalid votes and order secondary majority election ballots in bundle review

### 🔄 Changed

- fix voter participation rounding in protocols

### 🔄 Changed

- fix voter participation rounding in protocols

### 🆕 Added

- add secondary election detail protocols

### 🔄 Changed

- ignore hide lower domain of influence in report when same tenant in detail protocols

### 🔄 Changed

- support partial end results in vote end result protocol

### 🔄 Changed

- improve eCH-0252 write in candidate values

### 🔄 Changed

- correctly report eCH-0252 counting circle types

### 🔄 Changed

- correct calculation of voter participation with hide lower dois flag enabled in protocols

### 🆕 Added

- add counting circle to end result protocols where needed

### 🔄 Changed

- secondary election candidate end result state dependent of primary election result

### 🔄 Changed

- change column order for wabstic proportional election exports

### 🆕 Added

- add candidate title to eCH-0252 exports

### 🔄 Changed

- update pdf diff verified pdfs

### 🔄 Changed

- adjust proportional election wabstic exports

### 🔄 Changed

- correct counting circle validation in e-voting import

### 🔄 Changed

- don't export majority election write in candidates in eCH-0252 information export

### 🆕 Added

- add empty proportional list in eCH-0252 information export

### 🆕 Added

- add counting circle details and ballot results to result overview

### 🔄 Changed

- publish results for multiple submission finished

### 🆕 Added

- add empty vote count disabled flag to majority election protocols

### 🔄 Changed

- esult overview should contain both partial results and owned political businesses

### 🔄 Changed

- eCH-0252 always create XML even when no results are present

### 🔄 Changed

- export api list protocol for accessible political businesses

### 🆕 Added

- add pdf diff project

### 🔄 Changed

- fix(VOTING-5562): fix import processor for legacy events

### 🆕 Added

- add country, street and house number to election candidate

### 🔄 Changed

- fix protocol export event processing

### 🔄 Changed

- remove incorrect contests in contest list summary

### 🔄 Changed

- show contest in monitoring overview as manager without any political business to manage

### 🆕 Added

- add incumbent to proportional election candidates csv exports

### 🆕 Added

- added report level name

### 🆕 Added

- generic event watching

### 🔄 Changed

- generate vote end result report per domain of influence

### 🔄 Changed

- fix processing when missing voting cards get added and sub totals get removed

### 🆕 Added

- eCH-0252 proportional election list union added

### 🆕 Added

- e-counting write-in handling

### 🔄 Changed

- ensure valid majority election ballot

### 🔄 Changed

- e-counting import

### 🔄 Changed

- contest owners see all political businesses in eCH-0252 exports

### 🔄 Changed

- added additional vote infos to eCH-0252 tie break questions

### 🔄 Changed

- reverted routing changes in ExportController and ResultExportController

### 🔄 Changed

- routing of ExportController and ResultExportController. Rename of ResultExportController to ProtocolExportControler

### 🔄 Changed

- update basis majority election ballot groups event processing

### 🔄 Changed

- fix proportional election union end result election count sync
- fix election end result update with super apportionment lot decisions
