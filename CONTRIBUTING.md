Contributors
============

Submitting an issue? See the local README for the current fork support and issue-reporting guidance.

The process for contributions is roughly as follows:

## Prerequisites

 * Ensure you have completed any contributor agreement or internal approval process required by the current maintainer team before submitting non-trivial changes.

## Contributing Process

### Get Buyoff Or Find Open Community Issues/Features

 * Discuss the feature or bug with the current maintainer team through the workflow used by this fork.
 * If approved, ensure the accompanying issue is created with the necessary context and linked discussion.
 * Once you receive maintainer approval, you can start work on the feature.
 * If a feature is already marked as available for contribution, comment on the issue so work is not duplicated.

### Set Up Your Environment

 * Create or update a working branch from the repository maintained by the current publisher.
 * Clone the repository to your computer using the repository URL provided by the maintainer team.
 * If you also track the original upstream project, keep that remote clearly separated from the maintained fork.
 * From there you create a branch named specific to the feature. `git checkout -b feature-name`
 * In the branch you do work specific to the feature.
 * Please also observe the following:
    * No reformatting
    * No changing files that are not specific to the feature
    * More covered below in the **Prepare Commits** section.
 * Test your changes and please help us out by updating and implementing some automated tests. It is recommended that all contributors spend some time looking over the tests in the source code. You can't go wrong emulating one of the existing tests and then changing it specific to the behavior you are testing.
 * Please do not update your branch from the master unless we ask you to. See the **Respond to Feedback** section below.

### Prepare Commits

This section serves to help you understand what makes a good commit.

A commit should observe the following:

 * A commit is a small logical unit that represents a change.
 * Should include new or changed tests relevant to the changes you are making.
 * No unnecessary whitespace. Check for whitespace with `git diff --check` and `git diff --cached --check` before commit.
 * You can stage parts of a file for commit.

A commit message should observe the following:

  * The first line of the commit message should be a short description around 50 characters in length and be prefixed with the GitHub issue it refers to with parentheses surrounding that. If the GitHub issue is #25, you should have `(#25)` prefixed to the message. NOTE: Previously, the requirement was to use something like (GH-25) in commit messages, however, that approach has been deprecated.
  * If the commit is about documentation, the message should be prefixed with `(doc)`.
  * If it is a trivial commit or one of formatting/spaces fixes, it should be prefixed with `(maint)`.
  * After the subject, skip one line and fill out a body if the subject line is not informative enough.
  * The body:
    * Should indent at `72` characters.
    * Explains more fully the reason(s) for the change and contrasts with previous behavior.
    * Uses present tense. "Fix" versus "Fixed".

A good example of a commit message is as follows:

```
(#7) Installation Adds All Required Folders

Previously the installation script worked for the older version of
Chocolatey. It does not work similarly for the newer version of choco
due to location changes for the newer folders. Update the install
script to ensure all folder paths exist.

Without this change the install script will not fully install the new
choco client properly.
```

### Submit Pull Request (PR)

Prerequisites:

 * You are making commits in a feature branch.
 * All specs should be passing.

Submitting PR:

 * Once you feel it is ready, submit the pull request to the repository maintained for this fork against the branch required by the current maintainer workflow.
 * In the pull request, outline what you did and point to specific conversations (as in URLs) and issues that you are are resolving. This is a tremendous help for us in evaluation and acceptance.
 * Once the pull request is in, please do not delete the branch or close the pull request (unless something is wrong with it).
 * One of the maintainers or committers for this fork will evaluate it within a reasonable time period. Some things get evaluated faster or fast tracked. We do not commit to a formal SLA for pull requests.

### Respond to Feedback on Pull Request

We may have feedback for you to fix or change some things. We generally like to see that pushed against the same topic branch (it will automatically update the Pull Request). You can also fix/squash/rebase commits and push the same topic branch with `--force`. (It's generally acceptable to do this on topic branches not in the main repository. It is generally unacceptable and should be avoided at all costs against the main repository).

If we have comments or questions when we do evaluate it and receive no response, it will probably lessen the chance of getting accepted. Eventually this means it will be closed if it is not accepted. Please know this doesn't mean we don't value your contribution, just that things go stale. If in the future you want to pick it back up, feel free to address our concerns/questions/feedback and reopen the issue/open a new PR (referencing old one).

Sometimes we may need you to rebase your commit against the latest code before we can review it further. If this happens, you can do the following:

 * `git fetch upstream` (if you keep a separate remote for the original upstream project)
 * `git checkout develop`
 * `git rebase upstream/develop`
 * `git checkout your-branch`
 * `git rebase develop`
 * Fix any merge conflicts
 * `git push origin your-branch` and, if required by your workflow, `git push origin your-branch --force` for topic branches under your control.

The only reasons a pull request should be closed and resubmitted are as follows:

  * When the pull request is targeting the wrong branch (this doesn't happen as often).
  * When there are updates made to the original by someone other than the original contributor. Then the old branch is closed with a note on the newer branch this supersedes #github_number.

### Testing

There are some barebones Pester tests used to test the very basic functionalities of `chocolateyguicli`. These require Pester version 5.3.1 or newer. They can be launched by running `Invoke-Pester` within the `Tests` directory of the repository.

It is **not currently** expected that these Pester tests are run before submitting a PR. Their purpose at the moment is to establish a base to build upon.

### Branding and Release Maintenance

This repository supports branding-aware builds from a single pipeline.

 * Build profile argument: `--companyProfile=semight|nexustest` (default is `semight`).
 * Package id defaults to `instr-pkgmgr`, reserved for the unprefixed Semight Instruments package.
 * OEM builds must use `--packagePrefix=<prefix>` to keep package id, shim names, Inno AppId, install paths, runtime data paths, and cleanup scope isolated.
 * Optional `--oemId=<identity>` controls filesystem namespace and optional `--oemName=<display name>` controls publisher display metadata.

Version and release notes are team-maintained:

 * Update version baseline in `VERSION.txt`.
 * Update release notes in `CHANGELOG.md` for each release.
 * Keep package metadata release notes aligned with repository-maintained release notes.

### Debugging with Chocolatey library information

In order to debug this distribution, you need Chocolatey.Lib referenced in the project to match the Chocolatey version installed locally on your system. The easiest way to do this is to run `./Update-DebugConfiguration.ps1` from the root of the repository.

> :warning: **NOTE**
>
> You will need to have `nuget.commandline` installed for this script to work.
>
> You will also want to **not** commit the changes this script makes to the `.csproj` and `packages.config` files. As such, if you're making changes that would modify any of these files, it is recommended to make those changes, commit, then run the `./Update-DebugConfiguration.ps1` script.

## Other General Information

If you reformat code or change core functionality without maintainer approval, it is much less likely to be accepted. Reformatting code makes it harder to evaluate exactly what changed.

If you follow the guidelines we have above it will make evaluation and acceptance easy. If you stray from them it doesn't mean we are going to ignore your pull request, but it will make things harder for us. Harder for us roughly translates to a longer SLA for your pull request.
