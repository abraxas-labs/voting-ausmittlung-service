# ✨ Changelog (`v1.76.0`)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Info

```text
This version -------- v1.76.0
Previous version ---- 
Initial version ----- v1.29.14
Total commits ------- 18
```

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

### 🔄 Changed

- Update end result finalized on simple political business

### 🔄 Changed

- rename export protocols

### 🔄 Changed

- Some reports should only show up for certain types of domain of influences

### 🆕 Added

- add scoped dmdoc httpclient

### 🔒 Security

- Apply relaxed policy in transient catch up processor to handle replay attacks

### 🆕 Added

- add end result detail without empty and invalid votes protocol

### 🔄 Changed

- change voting card channel priority

### 🔄 Changed

- changed wabsti export column header

### 🔄 Changed

- change eCH-0222 import and test eCH export output

### ❌ Removed

- remove internal description, invalid votes and individual empty ballots allowed from elections

### 🔄 Changed

- hide proportional election end result columns and protocolls before finalized

### 🆕 Added

- Added export configuration political business metadata, needed for Seantis

### 🆕 Added

- add on list for proportional election candidate pdf exports

### 🔄 Changed

- update library to extend complex text input validation rules with dash sign

### 🔄 Changed

- Fixed handling of event signature on exports

### 🆕 Added

- add domain of influence canton

### 🔄 Changed

- Delete inherited domain of influence counting circles correctly on domain of influence delete

### 🆕 Added

- add candidate origin

### 🆕 Added

- add request recorder tooling for load testing playbook

### 🔄 Changed

- update voting lib to add transient subscription health check

### 🔒 Security

- Changed public key signing
- Validate voting basis event signature in activity protocol

### 🔄 Changed

- adjust input validation

### 🔄 Changed

- insert vote aggregated result correctly in protocols

### 🔄 Changed

- filter not needed domain of influence results in protocols

### 🆕 Added

- Added aggregated domain of influence results in protocols

### 🔄 Changed

- Removed contest details on end results and added domain of influence details in protocols

### 🆕 Added

- add result export configurations for newly created contests

### 🆕 Added

- added new vote counts to majority election

### 🆕 Added

- add log messages for debugging within the updated voting lib

### 🔄 Changed

- use unique identifier for messaging consumer endpoints so each horizontally scaled instance consumes change notifications
- ensure no proxy is used for local development so cert pins are matching

### 🆕 Added

- log messages for debugging

### 🆕 Added

- add eVoting write in mapping to invalid ballot

### 🆕 Added

- Added domain of influence and counting circle sort number to the protocols

### 🆕 Added

- add result state change listener for erfassung

### 🆕 Added

- Reset counting circle results in testing phase

### 🔄 Changed

- Changed WabstiC export

### 🔄 Changed

- Correctly register shared SECURE Connect account for DOK Connector

### 🔄 Changed

- WabstiC export changes

### 🔄 Changed

- no empty vote count for evoting import with single mandate

### 🔄 Changed

- Fixed summation of aggregated voting card results

### 🆕 Added

- Added DOK Connect implementation

### 🔄 Changed

- no empty vote count and no invalid vote count for single mandate

### 🆕 Added

- Added majority election calculation fields
- Added total count of voters on counting circle results in pdf protocols

### 🔄 Changed

- Send enum instead of a translated string as question label in pdf protocols

### 🆕 Added

- Added question labels in pdf protocols

### 🆕 Added

- Added pdf protocol field for counting circle and domain of influence name

### 🔄 Changed

- Deserialize eCH-0222 from eCH ballots, as the eCH votes may not correlate to the "VOTING votes"

### 🆕 Added

- Added name for protocol for domain of influence and counting circle
- Extended sorting of domain of influences and counting circles in protocols

### 🆕 Added

- second factor transaction code

### 🆕 Added

- review procedure for vote, majority election and proportional election

### 🆕 Added

- Add eCH message type to eCH-exports

### 🔄 Changed

- Correctly handle CountingCirclesMergerActivated events, which previously may not have created all necessary counting circles

### 🔒 Security

- Update proto validation dependencies

### 🆕 Added

- add Serilog.Expressions to exclude status endpoints from serilog request logging on success only

### 🆕 Added

- add application builder extension which is adding the serilog request logging middleware enriching the log context with tracability properties

### 🔄 Changed

- exchanged custom health check with ef core default one

### 🔄 Changed

- Set correct hagenbach bischoff distribution number

### 🔄 Changed

- Process political business number modification event of secondary majority election after testing phase has ended

### 🔄 Changed

- Updated proto validation dependencies

### 🔄 Changed

- Updated dependencies

### 🆕 Added

- Added proto validators at the requests.

### 🔄 Changed

- exchanged ef core default health check with custom one

### 🔄 Changed

- Allow contest counting circle details entry when e-voting is enabled

### 🔄 Changed

- Contests merge processing

### 🔄 Changed

- refactoring
- updated lib version

### 🔄 Changed

- correctly set new proportional election candidate party id on contest merge.

### 🆕 Added

- CORS configuration support

### 🔄 Changed

- refactored event signature

### 🔄 Changed

- refactored event signature and allow exceptions when deleting a public key

### 🔄 Changed

- upgraded underlying dotnet image to sdk 6.0.301 after gituhb issue [#24269](https://github.com/dotnet/sdk/issues/24269) has been fixed

### 🔄 Changed

- added OpenAPI description

### 🔄 Changed

- Fixes some code smells reported by sonar

### 🆕 Added

- add query split behavior where needed

### 🔒 Security

- Added authentication checks (role and correct tenant) to the methods which initialize the 2FA process

### 🆕 Added

- New proportional election union party mandates csv export

### 🔄 Changed

- Correctly map political business union id when returning templates

### 🔒 Security

- Added a check that requested political business union ids in exports have to be owned by the current tenant

### 🔄 Changed

- add cancellation token for verify second factor

### 🔄 Changed

- lot decision always required for proportional election when there are candidates with the same vote count

### 🔄 Changed

- get accessible counting circles only for the domain of influence from the current contest

### 🔄 Changed

- extend evoting date with time

The readmodel needs to be recreated after this commit

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
