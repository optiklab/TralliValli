# GitHub Status Checks Configuration

This document describes how to configure required status checks for the `build-test.yml` workflow on the `main` branch.

## Overview

The `build-test.yml` workflow runs automated tests on every pull request targeting the `main` branch. To ensure code quality, you should configure this workflow as a required status check.

## Prerequisites

- Repository admin access is required to configure branch protection rules
- The workflow must have run at least once on a pull request

## Configuration Steps

### 1. Navigate to Branch Protection Rules

1. Go to your repository on GitHub
2. Click on **Settings** (repository settings, not profile settings)
3. In the left sidebar, click **Branches**
4. Under "Branch protection rules", click **Add rule** (or edit an existing rule for `main`)

### 2. Configure the Protection Rule

1. In the "Branch name pattern" field, enter: `main`

2. Enable the following settings:
   - ☑️ **Require a pull request before merging**
     - Optional: Configure required approvals
   
   - ☑️ **Require status checks to pass before merging**
     - ☑️ **Require branches to be up to date before merging**
     - In the search box under "Status checks that are required", search for and select:
       - `build-and-test` (this is the job name from the workflow)

3. Optional but recommended settings:
   - ☑️ **Require conversation resolution before merging**
   - ☑️ **Include administrators** (apply rules to admins too)

4. Click **Create** (or **Save changes** if editing an existing rule)

## Verification

After configuration:

1. Create a test pull request to the `main` branch
2. The workflow should automatically run
3. The PR merge button should be blocked until the `build-and-test` check passes
4. You should see a status check indicator on the PR showing the workflow status

## Workflow Details

The `build-test.yml` workflow includes:

- **Checkout**: Clones the repository code
- **Setup .NET 8**: Installs .NET 8 SDK
- **Restore**: Restores NuGet package dependencies
- **Build**: Compiles the solution in Release configuration
- **Run unit tests**: Executes non-integration tests
- **Run integration tests**: Executes integration tests with Docker services via Testcontainers
- **Upload test results**: Stores test results as artifacts
- **Upload code coverage**: Stores code coverage reports as artifacts

## Docker Services

Integration tests use [Testcontainers](https://dotnet.testcontainers.org/) to automatically manage Docker containers for:

- MongoDB (database)
- RabbitMQ (message queue)
- Redis (caching)
- Azurite (Azure Storage emulator)

These containers are automatically started and stopped during test execution.

## Troubleshooting

### Status check not appearing

- Ensure the workflow has run at least once on a pull request
- Check that the workflow file is on the base branch (`main`)
- Verify the job name matches exactly: `build-and-test`

### Tests failing in CI but passing locally

- Check Docker service availability
- Verify network connectivity for NuGet restore
- Review test logs in the workflow run details
- Check uploaded test result artifacts for details

## Additional Resources

- [GitHub Branch Protection Documentation](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
- [Required Status Checks](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches#require-status-checks-before-merging)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
