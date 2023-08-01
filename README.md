# Tabrasa Serverless Repository
This repository is for Tabrasa serverless code, such as jobs and webhooks, typically written as Google Cloud Functions (GCFs).

### Branch Structure
This repo follows Tabrasa standards for branch handling:

* **master** is the primary branch, containing which should always have the latest released code
    * When releases are merged in to master, the merge should be tagged with the release number (ex: release/23.1.0)
* For each planned release, a new release branch should be created from master using that release's number as the branch name (ex: release/23.1.0)
    * If developing multiple releases at once, earlier release branches may be merged into later planned release branches to catch up
    * Once a release has completed and been merged back to master, master should be merged back into still open future release branches to catch them up
* For individual tasks such as work for Jira tickets, a branch should be created from the release branch the task is under
    * For JIRA tasks, the branch should start with the JIRA ticket ID, but may also have a description after (ex: ELO-1234_fixing_broken_json)
    * When done, the task branch is PRed into the Release branch it falls under (no direct checkins/merges on release branches!)

### File Structure
This repo follows Tabrasa standards for a "single concern" monorepo:

* Each individual releasable artifact should have it's own Folder at the parent level, named after the overall namespace used in the artifact (ex: Tabrasa.Serverless.StockDataSinkJob)
    * Each folder should contain the project file, code files, and a self-contained solution for that project that can be opened independently of others
* In the case where multiple code bases need to be opened and run simultaneously, a top-folder-level solution file may be added/used and have all necessary projects added
* Cloud deployment script files go into a special folder (in this case gcp) 