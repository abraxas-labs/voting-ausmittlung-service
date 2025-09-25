# ✨ Changelog (`v2.71.9`)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Info

```text
This version -------- v2.71.9
Previous version ---- v2.63.12
Initial version ----- v1.29.14
Total commits ------- 950
```

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

### 🔄 Changed

- validate counting circle details on all levels

### 🔄 Changed

- PoliticalBusinessResultProcessor: delete protocol export entries when state changes to CountingCircleResultState.AuditedTentatively

### 🔄 Changed

- correctly export candidates in eCH-0252

### 🆕 Added

- add eCH-0252 proportional election export with candidate list results info

### 🔄 Changed

- bump pkcs11 driver from 4.45 to 4.51.0.1

### 🔄 Changed

- support counting circle result reset with monitoring states

### 🔄 Changed

- fix super apportionment count of mandates for lotdecision

### 🔄 Changed

- filter eCH-0252 political business types correctly

### 🔄 Changed

- ResultExportTemplateReader: set protocol name regarding number of doi if TemplateModel is "PerDomainOfInfluence"

### 🔄 Changed

- do not export eCH-0252 swiss information for non-swiss candidates

### 🔄 Changed

- fix count of voters sub total sync on political business deletion

### 🔄 Changed

- eCH export languages dependent of contest e-voting

### 🔄 Changed

- export null instead of 0 for missing values in WabstiC

### 🔄 Changed

- fix permission check for bundle review protocol live updates

### 🔄 Changed

- show results when submission is done

### 🔄 Changed

- fix count of voters update on political business modifications

### 🆕 Added

- count of voters sub total with domain of influence type

### 🔄 Changed

- fix voter participation rounding in protocols

### 🔄 Changed

- refactor dockerfile and reduce cache layers

### 🔒 Security

- introduce user id and group id to avoid random assignment
- use exec form to avoid shell interpretation
