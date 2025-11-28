# ✨ Changelog (`v2.77.2`)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Info

```text
This version -------- v2.77.2
Previous version ---- v2.71.9
Initial version ----- v1.29.14
Total commits ------- 26
```

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

### 🔄 Changed

- speed up reading the list summary by only considering relevant contest permissions

### 🔄 Changed

- fix majority election end result detail protocol with individual candidates

### 🆕 Added

- add voteResultData to eCH-0252 export

### 🔄 Changed

- fix(VOTING-6460): do not sync entire DOI when political business is created, instead only create missing information

### 🔄 Changed

- static code analysis review

### 🆕 Added

- add proportional election union domain of influence in reports

### 🔄 Changed

- fix majority election candidate and ballot group basis event processing

### 🆕 Added

- add double proportional number of mandates excl lot decision

### 🆕 Added

- add configurable timeout for dok connector calls

### 🔄 Changed

- complete pipe writer so signal that no more data will be written to the underlying stream

### 🔄 Changed

- only export eCH-0252 party long description if it is present

### 🔄 Changed

- normalize attributes of type `GUID` according to event signature concept before generating binary payload

### 🆕 Added

- add majority election candidate reporting type

### 🆕 Added

- add party long description to majority election candidate

### 🔄 Changed

- optimize proportional election lot decisions

### 🔄 Changed

- update eCH-0252 version

### 🔄 Changed

- sort candidates per number instead of rank in list candidate votes end result protocol for zurich

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

### 🔄 Changed

- filter hagenbach bischoff protocols

### 🔄 Changed

- revert filter hagenbach bischoff protocols

### 🔄 Changed

- filter hagenbach bischoff protocols

### 🔄 Changed

- handle concurrent updates to contact person from Basis and Ausmittlung

### 🔄 Changed

- correctly sum counting circle details and adjust end results after state update

### 🆕 Added

- add proportional mandate algorithms to protocol xml

### 🔄 Changed

- for Models TieBreakQuestionEndResult, TieBreakQuestionResult, TieBreakQuestionDomainOfInfluenceResult  and Mapping VoteResultProfile: set PercentageQ2 only if TotalCountOfAnswerYes != 0 || TotalCountOfAnswerNo != 0 otherwise set to default = 0
- in Class VoteDomainOfInfluenceRsultBuilder for function ApplyQuestionResult: only count if if TotalCountOfAnswerYes != 0 || TotalCountOfAnswerNo != 0

### 🔄 Changed

- add gc compaction for specific requests as band-aid until memory issues with ech data exports are fixed

### 🔄 Changed

- for Models BallotQuestionEndResult, BallotQuestionResult, BallotQuestionDomainOfInfluenceResult and Mapping VoteResultProfile: set PercentageNo only if TotalCountOfAnswerYes != 0 || TotalCountOfAnswerNo != 0 otherwise set to default = 0
- in Class VoteDomainOfInfluenceRsultBuilder for function ApplyQuestionResult: only count if if TotalCountOfAnswerYes != 0 || TotalCountOfAnswerNo != 0

### 🔄 Changed

- ensure correct mandate distribution with previous audited tentatively versions

### 🆕 Added

- add 2FA to submission finished and audited tentatively methods

### 🔄 Changed

- do not require second factor in testing phase in specific cases

### 🔄 Changed

- consider correct domain of influence for partial results reporting level

### 🔄 Changed

- only show contests with active political businesses in monitoring

### 🔄 Changed

- filter all majority detail protocols with single counting circle result

### 🔄 Changed

- filter partial result protocols in export templates

### 🔄 Changed

- sum nullable ints as null if all values are null in WabstiC

### 🔄 Changed

- adjust reporting level on domain of influence protocols with partial results

### 🔄 Changed

- always require second factor

### 🔄 Changed

- add pdf domain of influence partial results flag

### 🔄 Changed

- change malware scanner config
