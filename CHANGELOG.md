# ✨ Changelog (`v1.39.1`)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Info

```text
This version -------- v1.39.1
Previous version ---- v1.36.4
Initial version ----- v1.29.14
Total commits ------- 13
```

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

## [v1.36.4] - 2022-08-22

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
- event signature metadata proto dependency

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

The readmodel needs to be recreated after this commit

## [v1.29.14] - 2022-05-19

### 🎉 Initial release for Bug Bounty
