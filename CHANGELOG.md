# ✨ Changelog (`v2.90.1`)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Info

```text
This version -------- v2.90.1
Previous version ---- v2.86.1
Initial version ----- v1.29.14
Total commits ------- 14
```

## [v2.90.1] - 2026-03-06

### 🔄 Changed

- fix(VOTING-6969): don't emit empty candidate text in ech 252

## [v2.90.0] - 2026-03-04

### 🆕 Added

- feat: detect db event processing lag and restart subscription if database is behind

## [v2.89.2] - 2026-02-26

### :arrows_counterclockwise: Changed

- Voting Lib Version -\> log level to warning for mapped exceptions with state in ExceptionInterceptor for grpc. Ticket reference: VOTING-6642, VOTING-6643, VOTING-6641

## [v2.89.1] - 2026-02-26

### 🔄 Changed

- allow empty vote ballots on variant ballots

## [v2.89.0] - 2026-02-25

### 🔄 Changed

- speed up majority election event processing

## [v2.88.0] - 2026-02-24

### :new: Added

- add parameter to pass async job priority for start async pdf generation
- use priorty from template repository to set bundle review protocols to 10

## [v2.87.4] - 2026-02-23

### :arrows_counterclockwise: Changed

- LanguageService method SetLanguage: Reduce Loglevel to debug for message: Language {lang} is not valid, using fallback language

## [v2.87.3] - 2026-02-20

### 🔄 Changed

- add majority election end result isComplete to PDF exports

## [v2.87.2] - 2026-02-19

### 🔄 Changed

- correctly set majority election end result in eCH-0252

## [v2.87.1] - 2026-02-16

### 🆕 Added

- add ballot modification users to bundles

## [v2.87.0] - 2026-02-16

### 🆕 Added

- save modified ballot numbers during review

## [v2.86.3] - 2026-02-12

### 🔄 Changed

- sort secondary elections in list candidates by political business number

## [v2.86.2] - 2026-02-10

### ❌ Removed

- remove export permissions for erfassung creator and bundle controller

## [v2.86.1] - 2026-02-06

### 🔄 Changed

- extend CD pipeline with enhanced bug bounty publication workflow

## [v2.86.0] - 2026-02-04

### 🔄 Changed

- speed up proportional election event processing

## [v2.85.2] - 2026-01-30

### 🔄 Changed

- live reload on double proportional lot decisions

## [v2.85.1] - 2026-01-29

### 🔄 Changed

- move metrics middleware to capture final status codes

## [v2.85.0] - 2026-01-27

### 🔄 Changed

- restrict election supporter permissions

## [v2.84.1] - 2026-01-21

### 🔄 Changed

- fix proportional election list vote source vote count in reports

## [v2.84.0] - 2026-01-19

### 🆕 Added

- add canton AR

## [v2.83.7] - 2026-01-19

### 🔄 Changed

- use transaction to prevent inconsistent data in exports

## [v2.83.6] - 2026-01-13

### 🔄 Changed

- correctly set bundle creator

## [v2.83.5] - 2026-01-12

### 🔄 Changed

- allow more roles to read all ballots

## [v2.83.4] - 2026-01-12

### 🆕 Added

- add proportional election vote count compared to number of mandates times accounted ballots validation

## [v2.83.3] - 2026-01-09

### 🔄 Changed

- allow generation of bundle review after review states

## [v2.83.2] - 2026-01-08

### 🔄 Changed

- set eCH-0252 isComplete correctly on secondary majority elections

## [v2.83.1] - 2026-01-08

### 🔄 Changed

- complete countOfVotersInformation for eCH-0252 votes

## [v2.83.0] - 2026-01-07

### 🔄 Changed

- deactivate check for unique bundle numbers

## [v2.82.9] - 2026-01-07

### 🔄 Changed

- add eCH-0252 proportional election candidate reference to elected

## [v2.82.8] - 2026-01-07

### 🔄 Changed

- sync election counts on union end result correctly

## [v2.82.7] - 2026-01-05

### 🔄 Changed

- allow more roles to update all bundles

## [v2.82.6] - 2025-12-19

### 🔄 Changed

- remove proportional election double proportional candidate lot decision

## [v2.82.5] - 2025-12-17

### 🔄 Changed

- renamed EventId property due to collision with Microsoft.Extensions.Logging's structured metadata object EventId

## [v2.82.4] - 2025-12-17

### 🆕 Added

- add majority election has candidates validation

## [v2.82.3] - 2025-12-17

### 🔄 Changed

- use same rounding algorithm as frontend

## [v2.82.2] - 2025-12-16

### :arrows_counterclockwise: Changed

- VoteInfoCandidateMapping: set default values like TG for canton ZH

###

## [v2.82.1] - 2025-12-11

### 🔄 Changed

- add voter numbers for all double proportional columns

## [v2.82.0] - 2025-12-11

### 🆕 Added

- set isMinor in eCH-0252 export

## [v2.81.0] - 2025-12-11

- omit date of birth in eCH-0252 if it is not set

### 🔄 Changed

- correctly set isElectionResultComplete in eCH-0252 export

### 🆕 Added

- manual ballot number generation

### 🔄 Changed

- ResultExportTemplateReader fix for Shortname extension in template description

### 🔄 Changed

- set finalized correctly on double proportional elections

### 🆕 Added

- add election lot decision state

### ❌ Removed

- remove missing majority election candidate result validation

- improve result bundles and ballots

## [v2.80.0] - 2025-12-10

### :arrows_counterclockwise: Changed

- DomainOfInfluenceCantonDataTransformer and VoteInfoCandidateMapping: implemented same logic like in basis -\> no candidate text for canton zh and no party in candidate text for canton tg

## [v2.79.0] - 2025-12-09

### 🆕 Added

- allow to import empty counting circle results

## [v2.78.0] - 2025-12-03

### 🔄 Changed

- allow eCH-0222 only import

(cherry picked from commit d078614501fcf3d2b7a1be77c405acb5989b6c82)

b4244ddf feat(VOTING-6510): allow eCH-0222 only import

Co-authored-by: Manuel Allenspach <manuel@riok.ch>

## [v2.77.2] - 2025-11-11

### 🔄 Changed

- speed up reading the list summary by only considering relevant contest permissions

## [v2.77.1] - 2025-11-06

### 🔄 Changed

- fix majority election end result detail protocol with individual candidates

## [v2.77.0] - 2025-11-04

### 🆕 Added

- add voteResultData to eCH-0252 export

## [v2.76.5] - 2025-10-23

### 🔄 Changed

- fix(VOTING-6460): do not sync entire DOI when political business is created, instead only create missing information

## [v2.76.4] - 2025-10-23

### 🔄 Changed

- static code analysis review

## [v2.76.3] - 2025-10-22

### 🆕 Added

- add proportional election union domain of influence in reports

## [v2.76.2] - 2025-10-22

### 🔄 Changed

- fix majority election candidate and ballot group basis event processing

## [v2.76.1] - 2025-10-20

### 🆕 Added

- add double proportional number of mandates excl lot decision

## [v2.76.0] - 2025-10-17

### 🆕 Added

- add configurable timeout for dok connector calls

### 🔄 Changed

- complete pipe writer so signal that no more data will be written to the underlying stream

## [v2.75.2] - 2025-10-16

### 🔄 Changed

- only export eCH-0252 party long description if it is present

## [v2.75.1] - 2025-10-14

### 🔄 Changed

- normalize attributes of type `GUID` according to event signature concept before generating binary payload

## [v2.75.0] - 2025-10-13

### 🆕 Added

- add majority election candidate reporting type

## [v2.74.0] - 2025-10-09

### 🆕 Added

- add party long description to majority election candidate

## [v2.73.1] - 2025-10-07

### 🔄 Changed

- optimize proportional election lot decisions

## [v2.73.0] - 2025-10-07

### 🔄 Changed

- update eCH-0252 version

## [v2.72.1] - 2025-10-03

### 🔄 Changed

- sort candidates per number instead of rank in list candidate votes end result protocol for zurich

## [v2.72.0] - 2025-09-30

### 🔄 Changed

- update proto
- update lib and and fix file wrapper interface change

### 🔄 Changed

- only export eCH-0252 proportional election elected when mandates are distributed

### 🔄 Changed

- support e-voting import per eCH-0222 v3

### 🔄 Changed

- enable optional fields in update counting circle details

### 🔄 Changed

- return 503 service unavailable when readiness endpoint returns degraded

### 🔄 Changed

- export candidate origin correctly

### 🔒 Security

- use raw event byte data instead of deserialized event data in signature verification

### 🔄 Changed

- mandate distribution auto-refresh and finalize fixes

## [v2.71.11] - 2025-09-30

### 🔄 Changed

- filter hagenbach bischoff protocols

## [v2.71.10] - 2025-09-26

### 🔄 Changed

- revert filter hagenbach bischoff protocols

## [v2.71.9] - 2025-09-25

### 🔄 Changed

- filter hagenbach bischoff protocols

## [v2.71.8] - 2025-09-24

### 🔄 Changed

- handle concurrent updates to contact person from Basis and Ausmittlung

## [v2.71.7] - 2025-09-23

### 🔄 Changed

- correctly sum counting circle details and adjust end results after state update

## [v2.71.6] - 2025-09-18

### 🆕 Added

- add proportional mandate algorithms to protocol xml

## [v2.71.5] - 2025-09-17

### 🔄 Changed

- use two phase compaction

## [v2.71.4] - 2025-09-16

### 🔄 Changed

- for Models TieBreakQuestionEndResult, TieBreakQuestionResult, TieBreakQuestionDomainOfInfluenceResult  and Mapping VoteResultProfile: set PercentageQ2 only if TotalCountOfAnswerYes != 0 || TotalCountOfAnswerNo != 0 otherwise set to default = 0
- in Class VoteDomainOfInfluenceRsultBuilder for function ApplyQuestionResult: only count if if TotalCountOfAnswerYes != 0 || TotalCountOfAnswerNo != 0

## [v2.71.3] - 2025-09-15

### 🔄 Changed

- add gc compaction for specific requests as band-aid until memory issues with ech data exports are fixed

## [v2.71.2] - 2025-09-12

### 🔄 Changed

- for Models BallotQuestionEndResult, BallotQuestionResult, BallotQuestionDomainOfInfluenceResult and Mapping VoteResultProfile: set PercentageNo only if TotalCountOfAnswerYes != 0 || TotalCountOfAnswerNo != 0 otherwise set to default = 0
- in Class VoteDomainOfInfluenceRsultBuilder for function ApplyQuestionResult: only count if if TotalCountOfAnswerYes != 0 || TotalCountOfAnswerNo != 0

## [v2.71.1] - 2025-09-04

### 🔄 Changed

- ensure correct mandate distribution with previous audited tentatively versions

## [v2.71.0] - 2025-09-03

### 🆕 Added

- add 2FA to submission finished and audited tentatively methods

## [v2.70.9] - 2025-08-29

### 🔄 Changed

- do not require second factor in testing phase in specific cases

## [v2.70.8] - 2025-08-28

### 🔄 Changed

- consider correct domain of influence for partial results reporting level

## [v2.70.7] - 2025-08-28

### 🔄 Changed

- only show contests with active political businesses in monitoring

## [v2.70.6] - 2025-08-27

### 🔄 Changed

- improve query performance of ListSummaries for owned political businesses

## [v2.70.5] - 2025-08-26

### 🔄 Changed

- filter all majority detail protocols with single counting circle result

## [v2.70.4] - 2025-08-26

### 🔄 Changed

- filter partial result protocols in export templates

## [v2.70.3] - 2025-08-22

### 🔄 Changed

- sum nullable ints as null if all values are null in WabstiC

## [v2.70.2] - 2025-08-20

### 🔄 Changed

- adjust reporting level on domain of influence protocols with partial results

## [v2.70.1] - 2025-08-20

### 🔄 Changed

- improve ListSummaries performance by adding an index

## [v2.70.0] - 2025-08-19

### 🔄 Changed

- always require second factor

## [v2.69.2] - 2025-08-14

### 🔄 Changed

- add pdf domain of influence partial results flag

## [v2.69.1] - 2025-08-14

### 🔄 Changed

- change malware scanner config

## [v2.69.0] - 2025-08-08

### 🆕 Added

- PublisherConfig: added new configuration EnableCantonSuffixTemplateKeys to set canton suffix to single reports. Needed for report splitting project.

### 🔄 Changed

- ProtocolExportService and ResultExportService: implement canton suffix for single protocol with new confiuration

### 🔄 Changed

- fix absolute majority threshold and other roundings in exports

### 🔄 Changed

- add listIndentureNumber to eCH-0252 export

### 🔄 Changed

- add candidateReferenceOnPosition to eCH-0252

## [v2.68.3] - 2025-07-25

### 🔄 Changed

- validate counting circle details on all levels

## [v2.68.2] - 2025-07-08

### 🔄 Changed

- PoliticalBusinessResultProcessor: delete protocol export entries when state changes to CountingCircleResultState.AuditedTentatively

## [v2.68.1] - 2025-07-03

### 🔄 Changed

- correctly export candidates in eCH-0252

## [v2.68.0] - 2025-07-03

### 🆕 Added

- add eCH-0252 proportional election export with candidate list results info

## [v2.67.7] - 2025-07-02

### 🔄 Changed

- bump pkcs11 driver from 4.45 to 4.51.0.1

## [v2.67.6] - 2025-06-30

### 🔄 Changed

- support counting circle result reset with monitoring states

## [v2.67.5] - 2025-06-30

### 🔄 Changed

- fix super apportionment count of mandates for lotdecision

## [v2.67.4] - 2025-06-30

### 🔄 Changed

- filter eCH-0252 political business types correctly

## [v2.67.3] - 2025-06-20

### 🔄 Changed

- ResultExportTemplateReader: set protocol name regarding number of doi if TemplateModel is "PerDomainOfInfluence"

## [v2.67.2] - 2025-06-20

### 🔄 Changed

- do not export eCH-0252 swiss information for non-swiss candidates

## [v2.67.1] - 2025-06-19

### 🔄 Changed

- fix count of voters sub total sync on political business deletion

## [v2.67.0] - 2025-06-18

### 🔄 Changed

- eCH export languages dependent of contest e-voting

## [v2.66.2] - 2025-06-17

### 🔄 Changed

- export null instead of 0 for missing values in WabstiC

## [v2.66.1] - 2025-06-17

### 🔄 Changed

- fix permission check for bundle review protocol live updates

## [v2.66.0] - 2025-06-16

### 🔄 Changed

- show results when submission is done

## [v2.65.1] - 2025-06-11

### 🔄 Changed

- fix count of voters update on political business modifications

## [v2.65.0] - 2025-06-02

### 🆕 Added

- count of voters sub total with domain of influence type

## [v2.64.1] - 2025-05-27

### 🔄 Changed

- fix voter participation rounding in protocols

## [v2.64.0] - 2025-05-27

### 🔄 Changed

- refactor dockerfile and reduce cache layers

### 🔒 Security

- introduce user id and group id to avoid random assignment
- use exec form to avoid shell interpretation

## [v2.63.13] - 2025-05-26

### 🔄 Changed

- include counting circle tenant ids in get result overview

### 🔄 Changed

- filter secondary election end result detail protocol correctly by canton settings

### 🔄 Changed

- delete protocols for deleted political businesses

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

### :new: Added

- validate eCH files on export

## [v2.53.0] - 2025-02-12

### 🔒 Security

- Pass a timeout to limit execution time and avoid ReDoS

### 🆕 Added

- add political business result bundle logs

### 🔄 Changed

- delete comments if counting circle results are resetted

### 🆕 Added

- add occupation to proportional election candidates csv exports

## [v2.52.0] - 2025-02-11

### :new: Added

- hide lower dois in reports

## [v2.51.0] - 2025-02-06

### :new: Added

- added support for procesing the HideLowerDoisInReports flag

## [v2.50.14] - 2025-02-06

### :arrows_counterclockwise: Changed

- revert adjust evoting test counting circles

## [v2.50.13] - 2025-01-30

### :arrows_counterclockwise: Changed

- always export eCH-0252 counting circles results, but filter result data

## [v2.50.12] - 2025-01-29

### :arrows_counterclockwise: Changed

- adjust evoting test counting circles

## [v2.50.11] - 2025-01-28

### :new: Added

- add VoteType to pdf reports

## [v2.50.10] - 2025-01-24

### 🔄 Changed

- add count of counting circle yes and no for domain of influence ballot results

## [v2.50.9] - 2025-01-23

### 🔄 Changed

- show majority election counting circle protocol always for canton ZH

## [v2.50.8] - 2025-01-23

### :arrows_counterclockwise: Changed

- correctly export eCH-0252 write in candidates values

## [v2.50.7] - 2025-01-14

### :arrows_counterclockwise: Changed

- export secondary majority election results that were not yet published

## [v2.50.6] - 2025-01-10

### 🔄 Changed

- update voting library from 12.20.0 to 12.22.3

### 🔒 Security

- use updated Pkcs11Interop library version 5.2.0

## [v2.50.5] - 2025-01-10

### 🔄 Changed

- contest manager can access all counting circles after testing phase ended

## [v2.50.4] - 2025-01-09

### 🔄 Changed

- flag results for correction during eVoting import in testing phase

## [v2.50.3] - 2024-12-20

### :arrows_counterclockwise: Changed

- do not write dash into eCH-0252 town if value is empty

## [v2.50.2] - 2024-12-20

### :arrows_counterclockwise: Changed

- log duplicated dependent entity type EF Core warnings as debug level

## [v2.50.1] - 2024-12-18

### 🔄 Changed

- adjust readable contest list summaries in erfassung

## [v2.50.0] - 2024-12-17

### 🔄 Changed

- update ech-0252 version

### 🆕 Added

- New Method in ResultExportTemplateReader: private async Task<bool> HasEvotingForDoiOnCountingCircle(Guid contestId) - if the doi is of type ch,ct,bz it will return true otherwise it returns true if at least one counting circle has e-voting enabled. default is false.

### 🔄 Changed

- ResultExportReader additional check for filtering e-voting protocols if no counting circle of the doi has e-voting enabled

## [v2.49.0] - 2024-12-16

### 🆕 Added

- include user id in log output

## [v2.48.2] - 2024-12-11

### 🔄 Changed

- majority election candidate optional values in active contest

## [v2.48.1] - 2024-12-05

### 🆕 Added

- add end result detail protocol for multiple counting circle results

## [v2.48.0] - 2024-12-04

### 🆕 Added

- add secondary majority election protocols

## [v2.47.4] - 2024-12-03

### 🔄 Changed

- add counting machine to vote overall result protocol

## [v2.47.3] - 2024-12-03

### ❌ Removed

- remove vote temporary end result protocol

## [v2.47.2] - 2024-12-03

### 🔄 Changed

- disable e-voting protocols if counting circle has no e-voting
- fixed tests on new voting lib rules for e-voting report visibility

## [v2.47.1] - 2024-11-29

### :arrows_counterclockwise: Changed

- sort results in vote details with evoting protocol

## [v2.47.0] - 2024-11-27

### 🆕 Added

- add secondary majority election candidate vote count validation

## [v2.46.0] - 2024-11-27

### ❌ Removed

- remove secondary majority election allowed candidates

### 🔄 Changed

- additional wabstic columns for majority elections

### 🔄 Changed

- optimize SourceLink integration and use new ci/cd versioning capabilities
- prevent duplicated commit ids in product version, only use SourceLink plugin.
- extend .dockerignore file with additional exclusions

### 🔄 Changed

- fix(VOTING-5094): calculate absolute majority for secondary elections separately

### 🆕 Added

- secondary majority election result validations

### 🔄 Changed

- feat(VOTING-4526): secondary elections on separate ballots

### 🆕 Added

- add proportional election list end result list lot decision updated to activity protocol

### 🔄 Changed

- archived contest list with description and owner column

### 🆕 Added

- publish results option on domain of influence

### 🔄 Changed

- eliminate duplicated queries in StartProtocolExports

## [v2.45.2] - 2024-11-08

### 🔄 Changed

- require all counting circle e-voting results during testing phase import

## [v2.45.1] - 2024-11-07

### 🔄 Changed

- update secondary election candidate translations correctly after update from referenced candidate

## [v2.45.0] - 2024-11-07

### 🆕 Added

- add reset to submission finished and flag for correction endpoints

## [v2.44.2] - 2024-11-06

### 🔄 Changed

- finish submission and audit not allowed for non communal political businesses

## [v2.44.1] - 2024-11-06

### 🔄 Changed

- double proportional calculation with exact rational calculations

## [v2.44.0] - 2024-11-06

### 🆕 Added

- optional rank in candidate lot decisions

## [v2.43.0] - 2024-11-04

### 🆕 Added

- add proportional election end result list lot decisions

## [v2.42.1] - 2024-11-04

### 🔄 Changed

- ech-0252 counting circle domain of influence types

## [v2.42.0] - 2024-11-04

### :new: Added

- added candidate list results info in eCH-0252

## [v2.41.0] - 2024-10-31

### :new: Added

- added voting card info to eCH-0252

## [v2.40.7] - 2024-10-30

### :arrows_counterclockwise: Changed

- remove origin and use occupation instead of occupation title in eCH-0252

## [v2.40.6] - 2024-10-30

### 🔄 Changed

- performance improvements for accessible contest list summaries

## [v2.40.5] - 2024-10-29

### 🔄 Changed

- fix protocol count of voters sum
- fix contest create domain of influence snapshots with superior authority

## [v2.40.4] - 2024-10-28

### 🆕 Added

- wabstic columns for ZH

## [v2.40.3] - 2024-10-25

### :arrows_counterclockwise: Changed

- remove title and add decisiveMajority named element to eCH-0252

## [v2.40.2] - 2024-10-24

### 🔄 Changed

- Eventprocessors: ContestProcessor, MajorityElectonProcessor, ProportionalElectionProcessor, Voteprocessor on deletion assume that object was already deleted if id was not found. In this case scip event processing.

## [v2.40.1] - 2024-10-23

### 🔄 Changed

- init eventing meter histogram to avoid no data alerts
- remove deprecated version from docker-compose

### 🔒 Security

- update Voting.Lib and according references to close Microsoft.Extensions.Caching.Memory vulnerability

## [v2.40.0] - 2024-10-23

### :arrows_counterclockwise: Changed

- election group position and other changes to eCH-0252

## [v2.39.2] - 2024-10-18

### :new: Added

- add counting machine to PDF exports

## [v2.39.1] - 2024-10-17

### :new: Added

- add ballot descriptions and vote result algorithm to PDF exports

## [v2.39.0] - 2024-10-15

### 🆕 Added

- superior authority domain of influence

## [v2.38.14] - 2024-10-14

### 🆕 Added

- add has ballot groups

## [v2.38.13] - 2024-10-11

### ❌ Removed

- remove counting circle detail validate endpoint

## [v2.38.12] - 2024-10-10

### 🔄 Changed

- filter multiple counting circle results for protocols

## [v2.38.11] - 2024-10-10

### 🔄 Changed

- inherited cc should not be deleted when multiple cc's exists on same doi tree

## [v2.38.10] - 2024-10-09

### :arrows_counterclockwise: Changed

- speed up event processing

## [v2.38.9] - 2024-10-04

### 🔄 Changed

- update proto version to apply new name input validation

## [v2.38.8] - 2024-10-03

### ❌ Removed

- remove zh feature flag

## [v2.38.7] - 2024-10-03

### 🔄 Changed

- resolved compiler warnings after updating analyzers

### ⚠️ Deprecated

- remove deprecated second factor code, instead use jwts tokens as primary codes

## [v2.38.6] - 2024-10-02

### 🔄 Changed

- eCH-0252 election elected mapping

## [v2.38.5] - 2024-10-02

### 🔄 Changed

- fix candidate text for majority election

## [v2.38.4] - 2024-10-01

### 🔄 Changed

- enter results as election supporter

## [v2.38.3] - 2024-09-30

### 🔄 Changed

- single mandate should throw if an empty vote is provided

## [v2.38.2] - 2024-09-27

### 🔄 Changed

- always load the counting circle results for the contest manager, whether the contest is in testing phase or active

## [v2.38.1] - 2024-09-27

### 🆕 Added

- add comment to delivery header

## [v2.38.0] - 2024-09-26

### 🆕 Added

- add candidate write in mapping and lot decisions

## [v2.37.4] - 2024-09-26

### 🔄 Changed

- only include active political businesses in eCH-0252

## [v2.37.3] - 2024-09-26

### :arrows_counterclockwise: Changed

- change rate limit reached status code

## [v2.37.2] - 2024-09-26

### 🔄 Changed

- add additional vote info to ech-0252 variant ballots

## [v2.37.1] - 2024-09-25

### 🔄 Changed

- double proportional algorithm run into an error, if there was more lists than political businesses

## [v2.37.0] - 2024-09-25

### 🆕 Added

- foreigner and minor voters

## [v2.36.1] - 2024-09-23

### 🔄 Changed

- truncate election candidate number

### 🔄 Changed

- file name and export name for other domain of influence types changed to sonstige

### 🔄 Changed

- added Description to ProportionalElectionUnionList

## [v2.36.0] - 2024-09-12

### 🔄 Changed

- consider testing phase in testDeliveryFlag

## [v2.35.1] - 2024-09-11

### 🔄 Changed

- move federal identification to ballot question

## [v2.35.0] - 2024-09-06

### 🆕 Added

- add federal identification

## [v2.34.0] - 2024-09-06

### :new: Added

- implement eCH-0252 sequence

## [v2.33.2] - 2024-09-05

### 🔄 Changed

- result submission finished to audited tentatively for owned results

## [v2.33.1] - 2024-09-04

### 🔄 Changed

- migrate from gcr to harbor

## [v2.33.0] - 2024-09-04

### 🆕 Added

- add correction finished and audited tentatively endpoint

## [v2.32.6] - 2024-09-03

### 🔄 Changed

- preserve ballot question results after update

## [v2.32.5] - 2024-09-02

### 🔄 Changed

- update ausmittlung proto library to increase limit for candidate position

## [v2.32.4] - 2024-08-30

### 🔄 Changed

- read only accessible counting circle results

## [v2.32.3] - 2024-08-29

### :arrows_counterclockwise: Changed

- reset counting circle results when it is in an incorrect state

## [v2.32.2] - 2024-08-29

### 🔄 Changed

- majority election result order by count for submission finished

## [v2.32.1] - 2024-08-28

### 🔄 Changed

- eCH-0252 counting circle domain of influence type

## [v2.32.0] - 2024-08-28

### 🆕 Added

- optional individual candidates on majority elections

## [v2.31.7] - 2024-08-28

🔄 Changed

update bug bounty template reference
patch ci-cd template version, align with new defaults

## [v2.31.6] - 2024-08-28

### 🔄 Changed

- saint lague rounding fix

## [v2.31.5] - 2024-08-23

### 🆕 Added

- add superior authority to eCH-0252

## [v2.31.4] - 2024-08-22

### 🔄 Changed

- move environment specific app settings out of default file

## [v2.31.3] - 2024-08-22

### :arrows_counterclockwise: Changed

- speed up processing of permission related events

## [v2.31.2] - 2024-08-22

### 🔄 Changed

- allow empty vote count for single mandate secondary majority election

## [v2.31.1] - 2024-08-21

### 🔄 Changed

- partial end result exports

## [v2.31.0] - 2024-08-20

### 🆕 Added

- add 2fa fallback qr code

## [v2.30.4] - 2024-08-20

### 🔄 Changed

- ensure swagger generator can be disabled completely

## [v2.30.3] - 2024-08-16

### 🔄 Changed

- counting circles need a political business result to be accesible

## [v2.30.2] - 2024-08-15

### :arrows_counterclockwise: Changed

- use consistent question id in vote eCH-0252 export

## [v2.30.1] - 2024-08-15

### 🆕 Added

- add eCH-0252 info export templates

## [v2.30.0] - 2024-08-14

### 🆕 Added

- add asynchronous bundle review

## [v2.29.0] - 2024-08-14

### 🆕 Added

- eCH-0252 base delivery

## [v2.28.2] - 2024-08-13

### :arrows_counterclockwise: Changed

- export variant votes on multiple ballots in eCH-0252 correctly

## [v2.28.1] - 2024-08-09

### 🔄 Changed

- eCH-0252 vote delivery always with counting circle infos

## [v2.28.0] - 2024-08-09

### :new: Added

- added political business and ballot sub type

## [v2.27.5] - 2024-08-08

### 🔄 Changed

- Updated the VotingLibVersion property in the Common.props file from 12.10.4 to 12.10.5. This update includes improvements for the proto string validation for better error reporting.

## [v2.27.4] - 2024-08-07

### 🔒 Security

- ech-0252 export per canton permissions

## [v2.27.3] - 2024-08-07

### 🔒 Security

- add restriction for import data and multipart form section content types

## [v2.27.2] - 2024-08-06

### 🔄 Changed

- reset bundle numbers if result is reset

## [v2.27.1] - 2024-08-05

### 🔄 Changed

- improve performance and memory allocation in certain event processors

## [v2.27.0] - 2024-07-30

### :new: Added

- added variant ballot on multiple ballots

## [v2.26.0] - 2024-07-26

### 🔄 Changed

- Make DOI short name optional in eCH-0252

## [v2.25.0] - 2024-07-19

### 🆕 Added

- canton settings with publish results before audited tentatively

## [v2.24.0] - 2024-07-16

### 🔄 Changed

- set counting circle e-voting at a specific date

## [v2.23.2] - 2024-07-16

### 🔄 Changed

- ech-0252 improvements

## [v2.23.1] - 2024-07-15

### 🔒 Security

- upgrade npgsql to fix vulnerability CVE-2024-0057

## [v2.23.0] - 2024-07-11

### :arrows_counterclockwise: Changed

- show protocol exports for political businesses which are not yet finalized

###

## [v2.22.2] - 2024-07-10

### 🔄 Changed

- calculation of select divisors in double proportional results

## [v2.22.1] - 2024-07-05

### 🔄 Changed

- result overview with partial results

## [v2.22.0] - 2024-07-05

### 🆕 Added

- add bundle state to pdf exports

## [v2.21.0] - 2024-07-04

### 🆕 Added

- add export template key canton suffix option

## [v2.20.5] - 2024-07-04

### 🔄 Changed

- update voting library to implement case-insensitivity for headers as per RFC-2616

## [v2.20.4] - 2024-07-03

### 🔄 Changed

- ExpandMultiplePoliticalBusinesses in ResultExportTemplateReader ensure that no ResultExportTemplate will be generated on empty political business.
- extended template tests for political businesses in finalized state

## [v2.20.3] - 2024-07-02

### 🔄 Changed

- show ech-0252 main vote id on the 2nd question

## [v2.20.2] - 2024-07-01

### 🔄 Changed

- ech-0252 export improvements

## [v2.20.1] - 2024-06-25

### 🔄 Changed

- ech-0252 export improvements

## [v2.20.0] - 2024-06-25

### 🔄 Changed

- create zip file with time zone info

## [v2.19.3] - 2024-06-24

### 🆕 Added

- add candidate check digit to candidate exports

## [v2.19.2] - 2024-06-24

### 🔄 Changed

- add index to improve contest list summaries performance

## [v2.19.1] - 2024-06-21

### :arrows_counterclockwise: Changed

- fix result bundle warnings

## [v2.19.0] - 2024-06-21

### 🆕 Added

- explicit election mandate distribution

### 🔄 Changed

- end result workflow

## [v2.18.5] - 2024-06-20

### 🆕 Added

- add partial results to result overview

## [v2.18.4] - 2024-06-14

### 🔄 Changed

- ech-252 export api adjustments

## [v2.18.3] - 2024-06-14

### :arrows_counterclockwise: Changed

- speed up reading of owned political businesses

## [v2.18.2] - 2024-06-12

### 🔄 Changed

- reset aggreated voting cards after electorates update

## [v2.18.1] - 2024-06-11

### 🔄 Changed

- update total count of voters and create missing voting cards when political business is created

## [v2.18.0] - 2024-06-07

### 🆕 Added

- add ready for correction timestamp

## [v2.17.4] - 2024-06-07

### :arrows_counterclockwise: Changed

- do not assume that a variant ballot with multiple questions has tie break questions

## [v2.17.3] - 2024-06-04

### 🔒 Security

- permissions on ech-0252

## [v2.17.2] - 2024-05-31

### 🔄 Changed

- ballot after testing phase updated

## [v2.17.1] - 2024-05-29

### 🔄 Changed

- split ech-0252 election to majority and proportional election export

## [v2.17.0] - 2024-05-29

### 🆕 Added

- add published state to results

## [v2.16.1] - 2024-05-27

### 🔄 Changed

- double proportional lot decision fixes

## [v2.16.0] - 2024-05-22

### 🆕 Added

- add ballot question type

## [v2.15.1] - 2024-05-17

### 🆕 Added

- testing utilities for double proportional results

## [v2.15.0] - 2024-05-16

### 🆕 Added

- double proportional lot decisions

## [v2.14.1] - 2024-05-14

### 🔄 Changed

- changed current date on pdf report names to contest date for all pdf exports

## [v2.14.0] - 2024-05-08

### :new: Added

- added eCH-0252 for elections

## [v2.13.0] - 2024-05-07

### 🔄 Changed

- allow to add same counting circle in domain of influence trees

## [v2.12.0] - 2024-05-07

### 🆕 Added

- update mandate algorithm for proportional elections in unions

## [v2.11.0] - 2024-05-03

### 🆕 Added

- ech-0252 export api

## [v2.10.0] - 2024-04-30

### 🆕 Added

- set multiple bundles to review succeed

## [v2.9.0] - 2024-04-26

### 🆕 Added

- double proportional election protocol

## [v2.8.1] - 2024-04-24

### 🔄 Changed

- move canton defaults from doi to contest

## [v2.8.0] - 2024-04-23

### 🔄 Changed

- rework monitoring cockpit overview

## [v2.7.0] - 2024-04-23

### 🆕 Added

- non cantonal double proportional result

## [v2.6.0] - 2024-04-19

### 🆕 Added

- add state plausibilised disabled canton setting

## [v2.5.0] - 2024-04-18

### 🆕 Added

- add counting circle result state descriptions

## [v2.4.3] - 2024-04-18

### 🔄 Changed

- support reset elections for all double proportional mandate algos

## [v2.4.2] - 2024-04-18

### 🔄 Changed

- remove temporary tenant for 2fa transaction confirmation authorization
- update VOTING IAM API client

## [v2.4.1] - 2024-04-17

### :arrows_counterclockwise: Changed

- handle vote without ballots

## [v2.4.0] - 2024-04-17

### :new: Added

- added vote end to end test

## [v2.3.0] - 2024-04-15

### 🆕 Added

- data and protocol export api

### :new: Added

- added partial end results

### ❌ Removed

- remove unions from election end result

### 🆕 Added

- proportional election union double proportional result protocols

- cantonal proportional election union results

- added eCH-0252 export (currently vote only)

- add political business unions to end result

- add political business unions to result overview

## [v2.2.0] - 2024-04-08

### 🆕 Added

- add evoting counting circle

## [v2.1.4] - 2024-04-05

### 🔄 Changed

- union list export order by order number

## [v2.1.3] - 2024-04-05

### 🔄 Changed

- update proportional election candidate results with vote sources template filename

## [v2.1.2] - 2024-04-04

### 🔄 Changed

- group union lists by short description for export

## [v2.1.1] - 2024-04-02

### 🔄 Changed

- validations consider conventional and e-voting results
- contest testing phase ended resets e-voting imported flag

## [v2.1.0] - 2024-03-21

### 🆕 Added

- add double proportional export templates

## [v2.0.0] - 2024-03-15

### :lock: Security

- dependency and runtime patch policy
- use latest dotnet runtime v8.0.3

### 🆕 Added

- add wp listen gde sk stat export

### 🔄 Changed

- majority election candidates bundle review order

### ❌ Removed

- voter turnout protocol export

### 🆕 Added

- add monitoring political business overview

### 🆕 Added

- add vote result algorithm popular and counting circle majority

### 🔄 Changed

- show wp gemeinden sk state export for every canton

### :new: Added

- added new roles

### 🆕 Added

- add list votes end result union export

- add submission finished and audited tentatively endpoint

BREAKING CHANGE: Updated service to .NET 8 LTS.

### :arrows_counterclockwise: Changed

- update to dotnet 8

### :lock: Security

- apply patch policy

- round voter participation to 6 decimal places

## [v1.109.0] - 2024-02-28

### 🔄 Changed

- count asynchronous protocol exports with invalid callback token separately

## [v1.108.0] - 2024-02-28

### :new: Added

- write in mapping change listener

## [v1.107.0] - 2024-02-27

### :arrows_counterclockwise: Changed

- adjust write in handling

## [v1.106.0] - 2024-02-23

### 🆕 Added

- add monitoring of asynchronous protocol exports

## [v1.105.0] - 2024-02-20

### 🔄 Changed

- report suffix for business level bz is "kantonal" instead of "bezirk"

## [v1.104.1] - 2024-02-20

### 🔄 Changed

- Enable electorates for non-zh

## [v1.104.0] - 2024-02-19

### 🔄 Changed

- Import ech-0110 count of voters informations

## [v1.103.0] - 2024-02-19

### 🆕 Added

- Add proportional wabsti exports with a single political business

## [v1.102.2] - 2024-02-07

### 🔄 Changed

- Group lists in proportional election unions

## [v1.102.1] - 2024-02-07

### 🆕 Added

- extend domain of influence type mapping with bezirk for report display name

## [v1.102.0] - 2024-02-06

### 🆕 Added

- Double proportional election mandate algorithms

## [v1.101.0] - 2024-02-05

### 🆕 Added

- database query monitoring

## [v1.100.4] - 2024-02-05

### 🔄 Changed

- proportional election union party votes export

## [v1.100.3] - 2024-02-01

### :arrows_counterclockwise: Changed

- exports generated for export configuration should use same state of data

## [v1.100.2] - 2024-02-01

### 🔄 Changed

- Filter out votes with no e-voting results in detail e-voting protocol

## [v1.100.1] - 2024-01-31

### 🔄 Changed

- Label in sk stat csv export

## [v1.100.0] - 2024-01-31

### 🆕 Added

- Add counting circle electorate

## [v1.99.1] - 2024-01-31

### :arrows_counterclockwise: Changed

- correctly check write-ins with their ballot content

## [v1.99.0] - 2024-01-30

### 🆕 Added

- add wp gemeinden sk stat export

## [v1.98.0] - 2024-01-29

### 🆕 Added

- add candidate check digit

## [v1.97.1] - 2024-01-25

### :arrows_counterclockwise: Changed

- adjusted proportional election end result protocols

## [v1.97.0] - 2024-01-16

### 🆕 Added

- add new zh features flag

## [v1.96.0] - 2024-01-11

### :new: Added

- added permission service

## [v1.95.3] - 2024-01-05

### 🔄 Changed

- csv proportional election candidates exports order

## [v1.95.2] - 2024-01-05

### 🔄 Changed

- Timestamp handling with result corrections

## [v1.95.1] - 2024-01-04

### 🔄 Changed

- csv proportional election candidates exports

## [v1.95.0] - 2024-01-04

### :lock: Security

- rework authentication system to use permissions instead of roles

## [v1.94.0] - 2023-12-20

### 🆕 Added

- Add counting machine to counting circle details

## [v1.93.0] - 2023-12-20

### 🆕 Added

- add eCH from voting lib

## [v1.92.0] - 2023-12-19

### 🆕 Added

- add multiple vote ballots

## [v1.91.9] - 2023-12-14

### 🔄 Changed

- adjust log level for abraxas authentication values

## [v1.91.8] - 2023-12-13

### 🔄 Changed

- use proportional election id for empty list identificationcurity

## [v1.91.7] - 2023-12-08

### :arrows_counterclockwise: Changed

- use separate port for metrics endpoint provisioning

## [v1.91.6] - 2023-12-05

### 🔄 Changed

- Delete protocol exports on counting circle reset

## [v1.91.5] - 2023-12-04

### :arrows_counterclockwise: Changed

- add additional oauth client scopes for subsystem access authorization

## [v1.91.4] - 2023-11-24

### :new: Added

- add support for custom oauth scopes.

## [v1.91.3] - 2023-11-23

### 🔄 Changed

- revert empty and invalid vote count for single majority mandate

## [v1.91.2] - 2023-11-17

### 🔄 Changed

- udpate to latest voting-lib version to fix role cache

## [v1.91.1] - 2023-11-17

### :arrows_counterclockwise: Changed

- add vote end results to e-voting details result export

## [v1.91.0] - 2023-11-15

### :new: Added

- add vote e-voting CSV report

## [v1.90.0] - 2023-11-10

### 🆕 Added

- add dmdoc callback fail policy
- add dmdoc callback timeout parameter

## [v1.89.0] - 2023-11-10

### :new: Added

- added vote e-voting details result protocols

## [v1.88.0] - 2023-11-09

### 🆕 Added

- added vote result e-voting protocol

## [v1.87.2] - 2023-11-08

### 🔄 Changed

- avoid raising of additional ProtocolExportCompleted events if aggregate state is already completed
- delegate draft cleanup to background job by enqueuing it to cleanup queue
- schedule draft content cleanup after successful callback
- schedule hard draft cleanup for obsolete documents

## [v1.87.1] - 2023-11-03

### 🆕 Added

- Add vote protocol e-voting fields

## [v1.87.0] - 2023-11-02

### 🔄 Changed

- update lib to add dmdoc callback retry

## [v1.86.10] - 2023-10-30

### 🔄 Changed

- clean up outdated draft on webhook callback

## [v1.86.9] - 2023-10-25

### 🔄 Changed

- correctly calculate count of modified lists for e-voting proportional elections

## [v1.86.8] - 2023-10-25

### 🔄 Changed

- correctly track e-voting vote sources

## [v1.86.7] - 2023-10-24

### 🔄 Changed

- use secury temporary file name for evoting uploads

## [v1.86.6] - 2023-10-23

### 🔄 Changed

- use latest lib to use new role token cache

## [v1.86.5] - 2023-10-20

### 🆕 Added

- add logs for webhook callback

## [v1.86.4] - 2023-10-20

### 🔄 Changed

- check if bundle exists before performing events on the bundle

## [v1.86.3] - 2023-10-20

### 🆕 Added

- Add e-voting proportional election list total results
- Add e-voting list end results to list union report

## [v1.86.2] - 2023-10-19

### 🔄 Changed

- Add missing evoting fields for protocols

## [v1.86.1] - 2023-10-19

### 🔄 Changed

- skip majority election ballot created if the bundle is deleted

## [v1.86.0] - 2023-10-18

### 🔄 Changed

- make PDF activity protocol smaller, add more detailed CSV version

## [v1.85.6] - 2023-10-17

### 🔄 Changed

- wabsti cwp list adjust zusatzstimmen

## [v1.85.5] - 2023-10-16

### 🔄 Changed

- wabstic wp gemeinde export total count of lists with party

## [v1.85.4] - 2023-10-13

### 🔄 Changed

- filter counting circle eVoting exports

## [v1.85.3] - 2023-10-11

### 🔄 Changed

- improve performance of ListSummaries

## [v1.85.2] - 2023-10-11

### 🔄 Changed

- Use correct eventing meter event position

## [v1.85.1] - 2023-10-10

### 🔄 Changed

- re-implement counting of pre-accumulated candidates again

## [v1.85.0] - 2023-10-10

### 🆕 Added

- add roles cache to minimize calls to iam

## [v1.84.1] - 2023-10-06

### 🔄 Changed

- Extend pdf proportional election ballot with whether all original candidates are removed from list

## [v1.84.0] - 2023-10-04

### 🔄 Changed

- upgrade voting library version to include event type processing histogram

## [v1.83.3] - 2023-10-03

### 🔄 Changed

- convert percentages in gemeinden export correctly

## [v1.83.2] - 2023-09-28

### 🔄 Changed

- revert counting pre-accumulated candidates in unmodified results

## [v1.83.1] - 2023-09-25

### 🔄 Changed

- Update lib to inject malware scanner config correctly

## [v1.83.0] - 2023-09-25

### 🆕 Added

- Add wp gemeinden bfs export

## [v1.82.0] - 2023-09-25

### 🔄 Changed

- Added eVoting protocols

## [v1.81.5] - 2023-09-15

### 🔄 Changed

- use empty value if absolut majority is not yet calculated in WabstiC WM_Kandidat csv export.

## [v1.81.4] - 2023-09-05

### 🔄 Changed

- handle completely empty proportional election lists correctly

## [v1.81.3] - 2023-09-04

### 🔄 Changed

- enable automatic exports during testing phase

## [v1.81.2] - 2023-08-31

### ❌ Removed

- malwarescanner - unless problem with cert-pinning is solved

## [v1.81.1] - 2023-08-30

### 🔄 Changed

- Skip processing of proportional election ballot create event if the bundle does not exist

## [v1.81.0] - 2023-08-22

### 🔄 Changed

- Update eai and lib dependency to deterministic version

## [v1.80.11] - 2023-08-22

### 🔄 Changed

- better support for large import files

## [v1.80.10] - 2023-08-22

### 🔄 Changed

- revert removal of ResultExportGenerated event

## [v1.80.9] - 2023-08-17

### 🔄 Changed

- increase import file size limit to 250MB

## [v1.80.8] - 2023-08-15

### 🆕 Added

- malwarescanner service

## [v1.80.7] - 2023-08-10

### 🆕 Added

- add sum of initial distribution number of mandates to pdf exports

## [v1.80.6] - 2023-08-04

### 🔄 Changed

- votes of ballots/bundles without a list should not count towards CountOfVotesOnOtherLists

## [v1.80.5] - 2023-07-26

### 🔄 Changed

- remove result export generated event and disable automatic exports during testing phase

## [v1.80.4] - 2023-07-20

### 🔄 Changed

- bundle review list without party

## [v1.80.3] - 2023-07-18

### 🔄 Changed

- rework party votes export

## [v1.80.2] - 2023-07-14

### ❌ Removed

- malwarescanner temporary unless resolved problem

## [v1.80.1] - 2023-07-13

### 🔄 Changed

- activity protocol export should only be available if contest manager, testing phase ended and only for monitoring

## [v1.80.0] - 2023-07-12

### ❌ Removed

- remove second factor transaction for owned political businesses

## [v1.79.0] - 2023-07-10

### 🆕 Added

- malware scanner service

## [v1.78.0] - 2023-06-28

### 🆕 Added

- add import change listener

## [v1.77.2] - 2023-06-23

### 🔄 Changed

- Extend wabsti csg abstimmungsergebnisse export with domain of influence type

## [v1.77.1] - 2023-06-23

### 🔄 Changed

- Sort contests depending on states

## [v1.77.0] - 2023-06-20

### 🆕 Added

- Multiple counting circle results submission finished

## [v1.76.5] - 2023-06-19

### 🔄 Changed

- Add missing events to activity protocol

## [v1.76.4] - 2023-06-19

### 🆕 Added

- Added modified lists count and lists without party count columns to csv proportional election candidates with vote sources export

## [v1.76.3] - 2023-06-18

### 🔄 Changed

- correct e-voting count of voters in CSV exports

## [v1.76.2] - 2023-06-18

### 🔄 Changed

- show e-voting count of voter values in reports

## [v1.76.1] - 2023-06-18

### 🔄 Changed

- remove filter on result algorithm in vote end result report

## [v1.76.0] - 2023-06-02

### 🔄 Changed

- add latest execution timestamp to result export configuration

## [v1.75.7] - 2023-05-31

### 🔄 Changed

- do not mark candidate results with optional lot decisions as pending

## [v1.75.6] - 2023-05-31

### 🔄 Changed

- add validation for when majority election has no candidates

## [v1.75.5] - 2023-05-30

### 🔄 Changed

- correctly handle repeated write ins reset

## [v1.75.4] - 2023-05-26

### 🔄 Changed

- Make certain contact person fields required

## [v1.75.3] - 2023-05-24

### 🔄 Changed

- submission finish race condition with updated counting circle details prevented

## [v1.75.2] - 2023-05-24

### 🆕 Added

- add db command timeout configuration

## [v1.75.1] - 2023-05-17

### 🔄 Changed

- Show correct read signed event count in activity protocol

## [v1.75.0] - 2023-05-16

### 🆕 Added

- reset write ins for majority election

## [v1.74.0] - 2023-05-16

### 🆕 Added

- add csv export for vote results

## [v1.73.0] - 2023-05-16

### 🔄 Changed

- moved creator from PdfMajorityElectionResultBundle to base class

## [v1.72.3] - 2023-05-15

### 🔄 Changed

- wabstic export vote id

## [v1.72.2] - 2023-05-15

### 🔄 Changed

- wabstic export vote id

## [v1.72.1] - 2023-05-09

### 🔄 Changed

- do not log update of lot decisions as error

## [v1.72.0] - 2023-05-08

### 🔄 Changed

- show imported counting circles

## [v1.71.1] - 2023-05-02

### 🔄 Changed

- update cd-templates to resolve blocking deploy-trigger

## [v1.71.0] - 2023-05-01

### 🔄 Changed

- correctly check imported voting cards contest ID
- import e-voting voting cards from eCH

## [v1.70.6] - 2023-04-25

### 🔄 Changed

- clear result values for initial state for wabstic majority election detail results report

## [v1.70.5] - 2023-04-24

### 🔄 Changed

- doi and cc sorting by name for protocols

## [v1.70.4] - 2023-04-19

### 🔄 Changed

- changed result export template entity description

## [v1.70.3] - 2023-04-18

### 🔄 Changed

- clear result values from certain states for wabstic majority election detail results report

## [v1.70.2] - 2023-04-13

### 🔄 Changed

- only report distinct ignored counting circles

## [v1.70.1] - 2023-04-06

### 🔄 Changed

- Make activity protocol for all monitoring admins available

## [v1.70.0] - 2023-04-05

### 🔄 Changed

- consider blank and invalid e-voting ballots for votes and proportional elections

## [v1.69.0] - 2023-03-31

### 🔄 Changed

- add e-voting blank ballots

## [v1.68.4] - 2023-03-29

### 🔄 Changed

- show correct count of voters information and voting cards on end results

## [v1.68.3] - 2023-03-27

### 🔄 Changed

- handle multiple eCH-0222 election group ballot raw data groups

## [v1.68.2] - 2023-03-24

### 🔄 Changed

- update voting lib to support eCH changes

## [v1.68.1] - 2023-03-17

### 🔄 Changed

- result start submission as contest manager should be possible

## [v1.68.0] - 2023-03-13

### 🔄 Changed

- allow enter results as contest manager in testing phase

## [v1.67.4] - 2023-03-12

### 🔄 Changed

- add the tenant ID to the export template ID

## [v1.67.3] - 2023-03-06

### 🔄 Changed

- restrict wabstic majority election detail results report to certain states

## [v1.67.2] - 2023-03-03

### 🔄 Changed

- use correct voting cards in communal voting end result report

## [v1.67.1] - 2023-03-02

### 🔄 Changed

- don't show multiple political businesses results when political business in not finalized

## [v1.67.0] - 2023-03-02

### 🔄 Changed

- protocol export state changes

## [v1.66.0] - 2023-03-01

### 🔄 Changed

- validate counting circles on result import and filter test counting circles

## [v1.65.1] - 2023-02-28

### 🔄 Changed

- fix list protocol exports

## [v1.65.0] - 2023-02-28

### 🔄 Changed

- WabstiC Majority election results only results with state correction done or submission done.

## [v1.64.0] - 2023-02-28

### 🔄 Changed

- async PDF generation process

## [v1.63.1] - 2023-02-23

### 🔄 Changed

- order candidate results for majority end result detail protocol by position

## [v1.63.0] - 2023-02-23

### 🔄 Changed

- VOTING-2480: input-validation allow character "«»;&

## [v1.62.0] - 2023-02-23

### 🔄 Changed

- wabstic wahlergebnisse additional columns

## [v1.61.1] - 2023-02-22

### 🔄 Changed

- order candidate results for majority end result detail protocol by position

## [v1.61.0] - 2023-02-20

### 🆕 Added

- wabstic wmwahlergebnis report

## [v1.60.3] - 2023-02-15

### 🔄 Changed

- wabstic use political names of candidates

## [v1.60.2] - 2023-02-13

### 🔄 Changed

- Update end result finalized on simple political business

## [v1.60.1] - 2023-02-13

### 🔄 Changed

- rename export protocols

## [v1.60.0] - 2023-02-10

### 🔄 Changed

- Some reports should only show up for certain types of domain of influences

## [v1.59.1] - 2023-02-08

### 🔄 Changed

- add more data to bundle review exports

## [v1.59.0] - 2023-02-01

### 🔄 Changed

- add invalid vote count to majority election result bundle review export

## [v1.58.2] - 2023-01-31

### 🔄 Changed

- expand multiple business counting circle templates correctly

## [v1.58.1] - 2023-01-31

### 🔄 Changed

- remove accumulated proportional election candidate from ballot candidates

## [v1.58.0] - 2023-01-31

### 🔄 Changed

- new export api

## [v1.57.0] - 2023-01-30

### 🔄 Changed

- detect replay attacks per activity protocol

## [v1.56.4] - 2023-01-26

### 🆕 Added

- add scoped dmdoc httpclient

## [v1.56.3] - 2023-01-25

### 🔄 Changed

- correctly copy result export configuration provider when creating a contest

## [v1.56.2] - 2023-01-25

### 🔄 Changed

- update library to fix dmdoc accessibility issues

## [v1.56.1] - 2023-01-24

### 🔄 Changed

- update library to use secure dmdoc authentication

## [v1.56.0] - 2023-01-23

### 🔄 Changed

- add basis events before testing phase ended to activity protocol

## [v1.55.2] - 2023-01-20

### 🔒 Security

- Apply relaxed policy in transient catch up processor to handle replay attacks

## [v1.55.1] - 2023-01-19

### 🔄 Changed

- clear audited tentatively timestamp on reset

## [v1.55.0] - 2023-01-18

### 🔄 Changed

- manual proportional election end result

## [v1.54.1] - 2023-01-13

### 🔄 Changed

- group seantis exports by seantis token

## [v1.54.0] - 2023-01-12

### 🔄 Changed

- add individual candidate to WabstiC WM_Kandidaten export

## [v1.53.3] - 2023-01-10

### 🔄 Changed

- order candidate results by vote count

## [v1.53.2] - 2023-01-10

### 🔄 Changed

- rename protocol description and filename

## [v1.53.1] - 2023-01-09

### 🔄 Changed

- sort counting circle results correctly in vote end result report

## [v1.53.0] - 2023-01-09

### 🔄 Changed

- add pdf ballot end result label

## [v1.52.0] - 2023-01-09

### 🔄 Changed

- allow empty ballots

## [v1.51.4] - 2023-01-09

### 🔄 Changed

- test eCH import against schema

## [v1.51.3] - 2023-01-06

### 🆕 Added

- add end result detail without empty and invalid votes protocol

## [v1.51.2] - 2023-01-05

### 🔄 Changed

- change voting card channel priority

## [v1.51.1] - 2023-01-05

### 🔄 Changed

- changed wabsti export column header

## [v1.51.0] - 2023-01-05

### 🔄 Changed

- change eCH-0222 import and test eCH export output

## [v1.50.2] - 2023-01-04

### ❌ Removed

- remove internal description, invalid votes and individual empty ballots allowed from elections

## [v1.50.1] - 2022-12-23

### 🔄 Changed

- hide proportional election end result columns and protocolls before finalized

## [v1.50.0] - 2022-12-23

### 🆕 Added

- Added export configuration political business metadata, needed for Seantis

## [v1.49.5] - 2022-12-20

### 🆕 Added

- add on list for proportional election candidate pdf exports

## [v1.49.4] - 2022-12-19

### 🔄 Changed

- update library to extend complex text input validation rules with dash sign

## [v1.49.3] - 2022-12-16

### 🔄 Changed

- Fixed handling of event signature on exports

## [v1.49.2] - 2022-12-16

### 🆕 Added

- add domain of influence canton

## [v1.49.1] - 2022-12-15

### 🔄 Changed

- Delete inherited domain of influence counting circles correctly on domain of influence delete

## [v1.49.0] - 2022-12-05

### 🆕 Added

- add candidate origin

## [v1.48.0] - 2022-12-02

### 🆕 Added

- add request recorder tooling for load testing playbook

## [v1.47.1] - 2022-11-30

### 🔄 Changed

- update voting lib to add transient subscription health check

## [v1.47.0] - 2022-11-29

### 🔒 Security

- Changed public key signing
- Validate voting basis event signature in activity protocol

## [v1.46.12] - 2022-11-29

### 🔄 Changed

- adjust input validation

## [v1.46.11] - 2022-11-27

### 🔄 Changed

- correctly export reports after testing phase has ended

## [v1.46.10] - 2022-11-24

### 🔄 Changed

- insert vote aggregated result correctly in protocols

## [v1.46.9] - 2022-11-23

### 🔄 Changed

- filter not needed domain of influence results in protocols

## [v1.46.8] - 2022-11-22

### 🆕 Added

- Added aggregated domain of influence results in protocols

### 🔄 Changed

- Removed contest details on end results and added domain of influence details in protocols

## [v1.46.7] - 2022-11-17

### 🔄 Changed

- ignore export of templates that do not exist (anymore)

## [v1.46.6] - 2022-11-09

### 🆕 Added

- add result export configurations for newly created contests

## [v1.46.5] - 2022-11-08

### 🆕 Added

- added new vote counts to majority election

## [v1.46.4] - 2022-11-07

### 🆕 Added

- add log messages for debugging within the updated voting lib

### 🔄 Changed

- use unique identifier for messaging consumer endpoints so each horizontally scaled instance consumes change notifications
- ensure no proxy is used for local development so cert pins are matching

### 🆕 Added

- log messages for debugging

## [v1.46.3] - 2022-11-04

### 🆕 Added

- add eVoting write in mapping to invalid ballot

## [v1.46.2] - 2022-11-02

### 🆕 Added

- Added domain of influence and counting circle sort number to the protocols

## [v1.46.1] - 2022-11-02

### 🆕 Added

- add result state change listener for erfassung

## [v1.46.0] - 2022-10-27

### 🆕 Added

- Reset counting circle results in testing phase

## [v1.45.5] - 2022-10-21

### 🔄 Changed

- Changed WabstiC export

## [v1.45.4] - 2022-10-19

### 🔄 Changed

- Correctly register shared SECURE Connect account for DOK Connector

## [v1.45.3] - 2022-10-19

### 🔄 Changed

- WabstiC export changes

## [v1.45.2] - 2022-10-17

### 🔄 Changed

- no empty vote count for evoting import with single mandate

## [v1.45.1] - 2022-10-14

### 🔄 Changed

- Fixed summation of aggregated voting card results

## [v1.45.0] - 2022-10-13

### 🆕 Added

- Added DOK Connect implementation

## [v1.44.2] - 2022-10-13

### 🔄 Changed

- no empty vote count and no invalid vote count for single mandate

## [v1.44.1] - 2022-10-11

### 🆕 Added

- Added majority election calculation fields
- Added total count of voters on counting circle results in pdf protocols

### 🔄 Changed

- Send enum instead of a translated string as question label in pdf protocols

## [v1.44.0] - 2022-10-11

### 🆕 Added

- Added question labels in pdf protocols

## [v1.43.2] - 2022-10-10

### 🆕 Added

- Added pdf protocol field for counting circle and domain of influence name

## [v1.43.1] - 2022-10-10

### 🔄 Changed

- Deserialize eCH-0222 from eCH ballots, as the eCH votes may not correlate to the "VOTING votes"

## [v1.43.0] - 2022-10-10

### 🆕 Added

- Added name for protocol for domain of influence and counting circle
- Extended sorting of domain of influences and counting circles in protocols

## [v1.42.0] - 2022-09-28

### 🆕 Added

- second factor transaction code

## [v1.41.0] - 2022-09-26

### 🆕 Added

- review procedure for vote, majority election and proportional election

## [v1.40.0] - 2022-09-23

### 🆕 Added

- Add eCH message type to eCH-exports

## [v1.39.2] - 2022-09-22

### 🔄 Changed

- Correctly handle CountingCirclesMergerActivated events, which previously may not have created all necessary counting circles

## [v1.39.1] - 2022-09-08

### 🔒 Security

- Update proto validation dependencies

## [v1.39.0] - 2022-09-06

### 🆕 Added

- add Serilog.Expressions to exclude status endpoints from serilog request logging on success only

## [v1.38.0] - 2022-09-05

### 🆕 Added

- add application builder extension which is adding the serilog request logging middleware enriching the log context with tracability properties

## [v1.37.5] - 2022-09-05

### 🔄 Changed

- exchanged custom health check with ef core default one

## [v1.37.4] - 2022-09-01

### 🔄 Changed

- Set correct hagenbach bischoff distribution number

## [v1.37.3] - 2022-08-31

### 🔄 Changed

- Process political business number modification event of secondary majority election after testing phase has ended

## [v1.37.2] - 2022-08-29

### 🔄 Changed

- Updated proto validation dependencies

## [v1.37.1] - 2022-08-29

### 🔄 Changed

- Updated dependencies

## [v1.37.0] - 2022-08-26

### 🆕 Added

- Added proto validators at the requests.

## [v1.36.5] - 2022-08-25

### 🔄 Changed

- exchanged ef core default health check with custom one

## [v1.36.4] - 2022-08-19

### 🔄 Changed

- Allow contest counting circle details entry when e-voting is enabled

## [v1.36.3] - 2022-08-16

### 🔄 Changed

- Contests merge processing

## [v1.36.2] - 2022-07-26

### 🔄 Changed

- refactoring
- updated lib version

## [v1.36.1] - 2022-07-22

### 🔄 Changed

- correctly set new proportional election candidate party id on contest merge.

## [v1.36.0] - 2022-07-13

### 🆕 Added

- CORS configuration support

## [v1.35.2] - 2022-07-12

### 🔄 Changed

- refactored event signature

## [v1.35.1] - 2022-07-12

### 🔄 Changed

- refactored event signature and allow exceptions when deleting a public key

## [v1.35.0] - 2022-06-27

### 🔄 Changed

- political business union party strength and voter participation export add new columns

## [v1.34.0] - 2022-06-27

### 🔄 Changed

- upgraded underlying dotnet image to sdk 6.0.301 after gituhb issue [#24269](https://github.com/dotnet/sdk/issues/24269) has been fixed

## [v1.33.8] - 2022-06-23

### 🔄 Changed

- added OpenAPI description

## [v1.33.7] - 2022-06-22

### 🔄 Changed

- add authorization checks where necessary

## [v1.33.6] - 2022-06-20

### 🔄 Changed

- Fixes some code smells reported by sonar

## [v1.33.5] - 2022-06-17

### 🔄 Changed

- fix code smells

## [v1.33.4] - 2022-06-14

### 🆕 Added

- add query split behavior where needed

## [v1.33.3] - 2022-06-13

### 🔒 Security

- Added authentication checks (role and correct tenant) to the methods which initialize the 2FA process

## [v1.33.2] - 2022-06-13

### 🔄 Changed

- use latest vo lib

## [v1.33.1] - 2022-06-10

### 🔄 Changed

- use new ssl cert option instead of preprocessor directive

## [v1.33.0] - 2022-06-07

### 🔄 Changed

- add proportional election union party votes report

## [v1.32.0] - 2022-06-07

### 🔄 Changed

- new proportional election union voter participation report

## [v1.31.0] - 2022-06-07

### 🆕 Added

- New proportional election union party mandates csv export

### 🔄 Changed

- Correctly map political business union id when returning templates

### 🔒 Security

- Added a check that requested political business union ids in exports have to be owned by the current tenant

## [v1.30.0] - 2022-06-02

### 🔄 Changed

- generate dotnet swagger docs

## [v1.29.24] - 2022-06-01

### 🔄 Changed

- add cancellation token for verify second factor

## [v1.29.23] - 2022-06-01

### 🔄 Changed

- lot decision always required for proportional election when there are candidates with the same vote count

## [v1.29.22] - 2022-05-31

### 🔄 Changed

- only change result state to in process if tenant matches

## [v1.29.21] - 2022-05-31

### 🔄 Changed

- avoid dividing by 0 in absolute majority calculation

## [v1.29.20] - 2022-05-25

### 🔄 Changed

- get accessible counting circles only for the domain of influence from the current contest

## [v1.29.19] - 2022-05-25

### 🔄 Changed

- extend evoting date with time

## [v1.29.18] - 2022-05-24

### 🔄 Changed

- contest merger should also merge simple businesses

## [v1.29.17] - 2022-05-23

### 🔄 Changed

- lib version

## [v1.29.16] - 2022-05-23

### 🔄 Changed

- add check for invalid value range

## [v1.29.15] - 2022-05-23

### 🔄 Changed

- code quality issues

## [v1.29.14] - 2022-05-19

### 🎉 Initial release for Bug Bounty
