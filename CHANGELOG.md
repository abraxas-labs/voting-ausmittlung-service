# ✨ Changelog (`v2.36.0`)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Info

```text
This version -------- v2.36.0
Previous version ---- v2.17.2
Initial version ----- v1.29.14
Total commits ------- 741
```

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

### 🔄 Changed

- ExpandMultiplePoliticalBusinesses in ResultExportTemplateReader ensure that no ResultExportTemplate will be generated on empty political business.
- extended template tests for political businesses in finalized state

### 🔄 Changed

- show ech-0252 main vote id on the 2nd question

### 🔄 Changed

- ech-0252 export improvements

### 🔄 Changed

- ech-0252 export improvements

### 🔄 Changed

- create zip file with time zone info

### 🆕 Added

- add candidate check digit to candidate exports

### :arrows_counterclockwise: Changed

- fix result bundle warnings

### 🆕 Added

- explicit election mandate distribution

### 🔄 Changed

- end result workflow

### 🆕 Added

- add partial results to result overview

### 🔄 Changed

- ech-252 export api adjustments

### :arrows_counterclockwise: Changed

- speed up reading of owned political businesses

### 🔄 Changed

- reset aggreated voting cards after electorates update

### 🔄 Changed

- update total count of voters and create missing voting cards when political business is created

### 🆕 Added

- add ready for correction timestamp

### :arrows_counterclockwise: Changed

- do not assume that a variant ballot with multiple questions has tie break questions

### 🔒 Security

- permissions on ech-0252

### 🔄 Changed

- ballot after testing phase updated

### 🔄 Changed

- split ech-0252 election to majority and proportional election export

### 🆕 Added

- add published state to results

### 🔄 Changed

- double proportional lot decision fixes

### 🆕 Added

- add ballot question type

### 🆕 Added

- testing utilities for double proportional results

### 🆕 Added

- double proportional lot decisions

### 🔄 Changed

- changed current date on pdf report names to contest date for all pdf exports

### :new: Added

- added eCH-0252 for elections

### 🔄 Changed

- allow to add same counting circle in domain of influence trees

### 🆕 Added

- update mandate algorithm for proportional elections in unions

### 🆕 Added

- ech-0252 export api

### 🆕 Added

- set multiple bundles to review succeed

### 🆕 Added

- double proportional election protocol

### 🔄 Changed

- move canton defaults from doi to contest

### 🔄 Changed

- rework monitoring cockpit overview

### 🆕 Added

- non cantonal double proportional result

### 🆕 Added

- add state plausibilised disabled canton setting

### 🆕 Added

- add counting circle result state descriptions

### 🔄 Changed

- support reset elections for all double proportional mandate algos

### 🔄 Changed

- remove temporary tenant for 2fa transaction confirmation authorization
- update VOTING IAM API client

### :arrows_counterclockwise: Changed

- handle vote without ballots

### :new: Added

- added vote end to end test

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

### 🆕 Added

- add evoting counting circle

### 🔄 Changed

- union list export order by order number

### 🔄 Changed

- update proportional election candidate results with vote sources template filename

### 🔄 Changed

- group union lists by short description for export

### 🔄 Changed

- validations consider conventional and e-voting results
- contest testing phase ended resets e-voting imported flag

### 🆕 Added

- add double proportional export templates

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

### 🔄 Changed

- count asynchronous protocol exports with invalid callback token separately

### :new: Added

- write in mapping change listener

### :arrows_counterclockwise: Changed

- adjust write in handling

### 🆕 Added

- add monitoring of asynchronous protocol exports

### 🔄 Changed

- report suffix for business level bz is "kantonal" instead of "bezirk"

### 🔄 Changed

- Enable electorates for non-zh

### 🔄 Changed

- Import ech-0110 count of voters informations

### 🆕 Added

- Add proportional wabsti exports with a single political business

### 🔄 Changed

- Group lists in proportional election unions

### 🆕 Added

- extend domain of influence type mapping with bezirk for report display name

### 🆕 Added

- Double proportional election mandate algorithms

### 🆕 Added

- database query monitoring

### 🔄 Changed

- proportional election union party votes export

### :arrows_counterclockwise: Changed

- exports generated for export configuration should use same state of data

### 🔄 Changed

- Filter out votes with no e-voting results in detail e-voting protocol

### 🔄 Changed

- Label in sk stat csv export

### 🆕 Added

- Add counting circle electorate

### :arrows_counterclockwise: Changed

- correctly check write-ins with their ballot content

### 🆕 Added

- add wp gemeinden sk stat export

### 🆕 Added

- add candidate check digit

### :arrows_counterclockwise: Changed

- adjusted proportional election end result protocols

### 🆕 Added

- add new zh features flag

### :new: Added

- added permission service

### 🔄 Changed

- csv proportional election candidates exports order

### 🔄 Changed

- Timestamp handling with result corrections

### 🔄 Changed

- csv proportional election candidates exports

### :lock: Security

- rework authentication system to use permissions instead of roles

### 🆕 Added

- Add counting machine to counting circle details

### 🆕 Added

- add eCH from voting lib

### 🆕 Added

- add multiple vote ballots

### 🔄 Changed

- adjust log level for abraxas authentication values

### 🔄 Changed

- use proportional election id for empty list identificationcurity

### :arrows_counterclockwise: Changed

- use separate port for metrics endpoint provisioning

### 🔄 Changed

- Delete protocol exports on counting circle reset

### :arrows_counterclockwise: Changed

- add additional oauth client scopes for subsystem access authorization

### :new: Added

- add support for custom oauth scopes.

### 🔄 Changed

- revert empty and invalid vote count for single majority mandate

### 🔄 Changed

- udpate to latest voting-lib version to fix role cache

### :arrows_counterclockwise: Changed

- add vote end results to e-voting details result export

### :new: Added

- add vote e-voting CSV report

### 🆕 Added

- add dmdoc callback fail policy
- add dmdoc callback timeout parameter

### :new: Added

- added vote e-voting details result protocols

### 🆕 Added

- added vote result e-voting protocol

### 🔄 Changed

- avoid raising of additional ProtocolExportCompleted events if aggregate state is already completed
- delegate draft cleanup to background job by enqueuing it to cleanup queue
- schedule draft content cleanup after successful callback
- schedule hard draft cleanup for obsolete documents

### 🆕 Added

- Add vote protocol e-voting fields

### 🔄 Changed

- update lib to add dmdoc callback retry

### 🔄 Changed

- clean up outdated draft on webhook callback

### 🔄 Changed

- correctly calculate count of modified lists for e-voting proportional elections

### 🔄 Changed

- correctly track e-voting vote sources

### 🔄 Changed

- use secury temporary file name for evoting uploads

### 🔄 Changed

- use latest lib to use new role token cache

### 🆕 Added

- add logs for webhook callback

### 🆕 Added

- Add e-voting proportional election list total results
- Add e-voting list end results to list union report

### 🔄 Changed

- Add missing evoting fields for protocols

### 🔄 Changed

- skip majority election ballot created if the bundle is deleted

### 🔄 Changed

- make PDF activity protocol smaller, add more detailed CSV version

### 🔄 Changed

- wabsti cwp list adjust zusatzstimmen

### 🔄 Changed

- wabstic wp gemeinde export total count of lists with party

### 🔄 Changed

- filter counting circle eVoting exports

### 🔄 Changed

- Use correct eventing meter event position

### 🆕 Added

- add roles cache to minimize calls to iam

### 🔄 Changed

- Extend pdf proportional election ballot with whether all original candidates are removed from list

### 🔄 Changed

- upgrade voting library version to include event type processing histogram

### 🔄 Changed

- convert percentages in gemeinden export correctly

### 🔄 Changed

- revert counting pre-accumulated candidates in unmodified results

### 🔄 Changed

- Update lib to inject malware scanner config correctly

### 🆕 Added

- Add wp gemeinden bfs export

### 🔄 Changed

- Added eVoting protocols

### 🔄 Changed

- use empty value if absolut majority is not yet calculated in WabstiC WM_Kandidat csv export.

### 🔄 Changed

- handle completely empty proportional election lists correctly

### 🔄 Changed

- enable automatic exports during testing phase

### ❌ Removed

- malwarescanner - unless problem with cert-pinning is solved

### 🔄 Changed

- Skip processing of proportional election ballot create event if the bundle does not exist

### 🔄 Changed

- Update eai and lib dependency to deterministic version

### 🔄 Changed

- revert removal of ResultExportGenerated event

### 🆕 Added

- malwarescanner service

### 🆕 Added

- add sum of initial distribution number of mandates to pdf exports

### 🔄 Changed

- votes of ballots/bundles without a list should not count towards CountOfVotesOnOtherLists

### 🔄 Changed

- remove result export generated event and disable automatic exports during testing phase

### 🔄 Changed

- bundle review list without party

### 🔄 Changed

- rework party votes export

### ❌ Removed

- malwarescanner temporary unless resolved problem

### 🔄 Changed

- activity protocol export should only be available if contest manager, testing phase ended and only for monitoring

### ❌ Removed

- remove second factor transaction for owned political businesses

### 🆕 Added

- malware scanner service

### 🆕 Added

- add import change listener

### 🔄 Changed

- Extend wabsti csg abstimmungsergebnisse export with domain of influence type

### 🔄 Changed

- Sort contests depending on states

### 🆕 Added

- Multiple counting circle results submission finished

### 🔄 Changed

- Add missing events to activity protocol

### 🆕 Added

- Added modified lists count and lists without party count columns to csv proportional election candidates with vote sources export

### 🔄 Changed

- Make certain contact person fields required

### 🔄 Changed

- submission finish race condition with updated counting circle details prevented

### 🆕 Added

- add db command timeout configuration

### 🔄 Changed

- Show correct read signed event count in activity protocol

### 🆕 Added

- reset write ins for majority election

### 🆕 Added

- add csv export for vote results

### 🔄 Changed

- moved creator from PdfMajorityElectionResultBundle to base class

### 🔄 Changed

- wabstic export vote id

### 🔄 Changed

- wabstic export vote id

### 🔄 Changed

- update cd-templates to resolve blocking deploy-trigger

### 🔄 Changed

- clear result values for initial state for wabstic majority election detail results report

### 🔄 Changed

- doi and cc sorting by name for protocols

### 🔄 Changed

- changed result export template entity description

### 🔄 Changed

- clear result values from certain states for wabstic majority election detail results report

### 🔄 Changed

- Make activity protocol for all monitoring admins available

### 🔄 Changed

- result start submission as contest manager should be possible

### 🔄 Changed

- allow enter results as contest manager in testing phase

### 🔄 Changed

- WabstiC Majority election results only results with state correction done or submission done.

### 🔄 Changed

- VOTING-2480: input-validation allow character "«»;&

### 🔄 Changed

- wabstic wahlergebnisse additional columns

### 🔄 Changed

- order candidate results for majority end result detail protocol by position

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

### :arrows_counterclockwise: Changed

- do not assume that a variant ballot with multiple questions has tie break questions

### 🔒 Security

- permissions on ech-0252

### 🔄 Changed

- ballot after testing phase updated

### 🔄 Changed

- split ech-0252 election to majority and proportional election export

### 🆕 Added

- add published state to results

### 🔄 Changed

- double proportional lot decision fixes

### 🆕 Added

- add ballot question type

### 🆕 Added

- testing utilities for double proportional results

### 🆕 Added

- double proportional lot decisions

### 🔄 Changed

- changed current date on pdf report names to contest date for all pdf exports

### :new: Added

- added eCH-0252 for elections

### 🔄 Changed

- allow to add same counting circle in domain of influence trees

### 🆕 Added

- update mandate algorithm for proportional elections in unions

### 🆕 Added

- ech-0252 export api

### 🆕 Added

- set multiple bundles to review succeed

### 🆕 Added

- double proportional election protocol

### 🔄 Changed

- move canton defaults from doi to contest

### 🔄 Changed

- rework monitoring cockpit overview

### 🆕 Added

- non cantonal double proportional result

### 🆕 Added

- add state plausibilised disabled canton setting

### 🆕 Added

- add counting circle result state descriptions

### 🔄 Changed

- support reset elections for all double proportional mandate algos

### 🔄 Changed

- remove temporary tenant for 2fa transaction confirmation authorization
- update VOTING IAM API client

### :arrows_counterclockwise: Changed

- handle vote without ballots

### :new: Added

- added vote end to end test

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

## [v2.2.1] - 2024-06-07

### :arrows_counterclockwise: Changed

- do not assume that a variant ballot with multiple questions has tie break questions

### 🔒 Security

- permissions on ech-0252

### 🔄 Changed

- ballot after testing phase updated

### 🔄 Changed

- split ech-0252 election to majority and proportional election export

### 🆕 Added

- add published state to results

### 🔄 Changed

- double proportional lot decision fixes

### 🆕 Added

- add ballot question type

### 🆕 Added

- testing utilities for double proportional results

### 🆕 Added

- double proportional lot decisions

### 🔄 Changed

- changed current date on pdf report names to contest date for all pdf exports

### :new: Added

- added eCH-0252 for elections

### 🔄 Changed

- allow to add same counting circle in domain of influence trees

### 🆕 Added

- update mandate algorithm for proportional elections in unions

### 🆕 Added

- ech-0252 export api

### 🆕 Added

- set multiple bundles to review succeed

### 🆕 Added

- double proportional election protocol

### 🔄 Changed

- move canton defaults from doi to contest

### 🔄 Changed

- rework monitoring cockpit overview

### 🆕 Added

- non cantonal double proportional result

### 🆕 Added

- add state plausibilised disabled canton setting

### 🆕 Added

- add counting circle result state descriptions

### 🔄 Changed

- support reset elections for all double proportional mandate algos

### 🔄 Changed

- remove temporary tenant for 2fa transaction confirmation authorization
- update VOTING IAM API client

### :arrows_counterclockwise: Changed

- handle vote without ballots

### :new: Added

- added vote end to end test

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
