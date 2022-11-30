# ✨ Changelog (`v1.47.1`)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Info

```text
This version -------- v1.47.1
Previous version ---- v1.42.0
Initial version ----- v1.29.14
Total commits ------- 28
```

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
