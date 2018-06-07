/**
 * JenkinsFile for the pipeline of Intelliflo.Platform projects
 * ~~ MANAGED BY DEVOPS ~~
 */

/**
 * By default the master branch of the library is loaded
 * Use the include directive below ONLY if you need to load a branch of the library
 * @Library('intellifloworkflow@IP-36076')
 */
import org.intelliflo.*

def changeset = new Changeset()
PipelineConfig pipelineConfig

def artifactoryCredentialsId = 'a3c63f46-4be7-48cc-869b-4239a869cbe8'
def artifactoryUri = 'https://artifactory.intelliflo.io/artifactory'
def gitCredentialsId = '1327a29c-d426-4f3d-b54a-339b5629c041'
def jiraCredentialsId = '32546070-393c-4c45-afcd-8e8f1de1757b'

def stageName
def semanticVersion
def packageVersion
def globals = env
def verboseLogging = false
def windowsNode = 'windows'

pipeline {

    agent none

    environment {
        githubRepoName = "${env.JOB_NAME.split('/')[1]}"
        solutionName = "${env.JOB_NAME.split('/')[1].replace('Clone.', '')}"
    }

    options {
        timestamps()
        skipDefaultCheckout()
    }

    stages {
        stage('Initialise') {
            agent none
            steps {
                script {
                    stashResourceFiles {
                        targetPath = 'org/intelliflo'
                        masterNode = 'master'
                        stashName = 'ResourceFiles'
                        resourcePath = "@libs/intellifloworkflow/resources"
                    }

                    abortOlderBuilds {
                        logVerbose = verboseLogging
                    }
                }
            }
        }

        stage('Component') {
            agent {
                label windowsNode
            }
            steps {
                bat 'set'

                script {
                    stageName = 'Component'

                    // Analyse and validate the changeset
                    validateChangeset {
                        repoName = globals.githubRepoName
                        solutionName = globals.solutionName
                        prNumber = globals.CHANGE_ID
                        baseBranch = globals.CHANGE_TARGET
                        branchName = globals.BRANCH_NAME
                        buildNumber = globals.BUILD_NUMBER
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                        abortOnFailure = true
                    }
                    def json = (String)Consul.getStoreValue(ConsulKey.get(globals.githubRepoName, globals.BRANCH_NAME, globals.CHANGE_ID, 'changeset'))
                    changeset = changeset.fromJson(json)

                    // Checkout the code and unstash supporting scripts
                    checkoutCode {
                        delegate.stageName = stageName
                    }

                    pipelineConfig = getPipelineConfig {
                        configFile = "Jenkinsfile-config.yml"
                    }

                    // Scripts required by the pipeline
                    unstashResourceFiles {
                        folder = 'pipeline'
                        stashName = 'ResourceFiles'
                    }

                    // Versioning
                    calculateVersion {
                        buildNumber = changeset.buildNumber
                        delegate.changeset = changeset
                        delegate.stageName = stageName
                        abortOnFailure = true
                        logVerbose = verboseLogging
                    }

                    semanticVersion = Consul.getStoreValue(ConsulKey.get(globals.githubRepoName, changeset.branchName, changeset.prNumber, 'existing.version'))
                    packageVersion = "${semanticVersion}.${globals.BUILD_NUMBER}"
                    if (changeset.pullRequest != null) {
                        currentBuild.displayName = "${globals.githubRepoName}.Pr${changeset.prNumber}(${packageVersion})"
                    } else {
                        currentBuild.displayName = "${globals.githubRepoName}(${packageVersion})"
                    }

                    startSonarQubeAnalysis {
                        repoName = globals.githubRepoName
                        solutionName = globals.solutionName
                        version = semanticVersion
                        branchName = changeset.originatingBranch
                        unitTestResults = "UnitTestResults"
                        coverageResults = "OpenCoverResults"
                        inspectCodeResults = "ResharperInspectCodeResults"
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    createVersionTargetsFile {
                        serviceName = globals.solutionName
                        version = packageVersion
                        sha = changeset.commitSha
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                        targetsFile = "version.justsaying.targets"
                    }

                    buildCode {
                        delegate.stageName = stageName
                        delegate.pipelineConfig = pipelineConfig
                        delegate.packageVersion = packageVersion
                        delegate.changeset = changeset
                    }

                    runDependencyCheck {
                        repoName = "${globals.solutionName}"
                        binariesLocation = "${globals.solutionName}\\bin\\Release"
                        delegate.stageName = stageName
                    }

                    def unitTestResults = runUnitTests {
                        title = "Unit Tests"
                        withCoverage = true
                        runner = "${pwd()}\\packages\\NUnit.ConsoleRunner.3.2.1\\tools\\nunit3-console.exe"
                        options = "--framework=4.0 --result=dist\\UnitTestResults.xml;format=nunit2"
                        recurse = true
                        include = "**/bin/Release/*UnitTests.dll"
                        unitTestsResultsFilename = "UnitTestResults"
                        coverageInclude = globals.solutionName
                        coverageResultsFilename = "OpenCoverResults"
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    runResharperInspectCode {
                        repoName = globals.githubRepoName
                        solutionName = globals.solutionName
                        resultsFile = "ResharperInspectCodeResults"
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    completeSonarQubeAnalysis {
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    analyseTestResults {
                        title = "Unit Tests"
                        testResults = unitTestResults
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    if (changeset.pullRequest != null) {
                        nugetPack {
                            fileSpec = "JustSayingIflo.nuspec"
                            version = packageVersion
                            configuration = 'Release'
                            artifactFolder = 'dist'
                            stashPackages  = true
                            stashName = 'package'
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }
                    }

                    nugetPack {
                        fileSpec = "JustSayingIflo.nuspec"
                        version = "${packageVersion}-alpha"
                        configuration = 'Release'
                        artifactFolder = 'dist'
                        stashPackages  = false
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    findAndDeleteOldPackages {
                        credentialsId = artifactoryCredentialsId
                        packageName = "JustSayingIflo.${semanticVersion}"
                        latestBuildNumber = changeset.buildNumber
                        repos = 'nuget-local'
                        url = artifactoryUri
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    def propset = ''
                    if (changeset.pullRequest != null) {
                        propset = "github.pr.number=${changeset.prNumber} git.repo.name=${changeset.repoName} git.master.mergebase=${changeset.masterSha} jira.ticket=${changeset.jiraTicket}"
                    }
                    if (changeset.branch != null) {
                        propset = "github.branch.name=${changeset.branchName} git.repo.name=${changeset.repoName} git.master.sha=${changeset.masterSha} jira.ticket=${changeset.jiraTicket}"
                    }
                    publishPackages {
                        credentialsId = artifactoryCredentialsId
                        repo = 'nuget-local'
                        version = "${packageVersion}-alpha"
                        include = "JustSayingIflo.${packageVersion}-alpha.nupkg"
                        uri = artifactoryUri
                        properties = propset
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }
                }
            }
            post {
                always {
                    script {
                        deleteWorkspace {
                            force = true
                        }
                    }
                }
            }
        }

        stage('Production') {
            agent none
            when {
                expression { env.BRANCH_NAME ==~ /^PR-.*/ }
            }
            steps {
                script {
                    stageName = 'Production'

                    validateBaseBranchSha {
                        repoName = changeset.repoName
                        baseBranch = changeset.baseBranch
                        packageSha = changeset.baseBranchSha
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    validateCodeReviews {
                        repoName = changeset.repoName
                        prNumber = changeset.prNumber
                        author = changeset.author
                        failBuild = false
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    validateJiraTicket {
                        delegate.changeset = changeset
                        failBuild = false
                        delegate.stageName = stageName
                        logVerbose = verboseLogging
                    }

                    node(windowsNode) {
                        mergePullRequest {
                            repoName = changeset.repoName
                            prNumber = changeset.prNumber
                            masterSha = changeset.masterSha
                            sha = changeset.commitSha
                            consulKey = changeset.consulBaseKey
                            credentialsId = gitCredentialsId
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        updateMasterVersion {
                            repoName = changeset.repoName
                            version = semanticVersion
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        tagCommit {
                            repoName = changeset.repoName
                            version = semanticVersion
                            author = changeset.author
                            email = changeset.commitInfo.author.email
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        unstashResourceFiles {
                            folder = 'pipeline'
                            stashName = 'ResourceFiles'
                        }

                        bat "if not exist dist\\NUL (mkdir dist)"
                        dir('dist') {
                            unstash 'package'
                        }

                        publishPackages {
                            credentialsId = artifactoryCredentialsId
                            repo = 'nuget-local'
                            version = packageVersion
                            include = "*.${packageVersion}.nupkg"
                            uri = artifactoryUri
                            properties = "github.pr.number=${changeset.prNumber} git.repo.name=${changeset.repoName} git.master.mergebase=${changeset.masterSha} jira.ticket=${changeset.jiraTicket}"
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        deletePackage {
                            repo = 'nuget-local'
                            packages = ["JustSayingIflo"]
                            version = "${packageVersion}-alpha"
                            credentialsId = artifactoryCredentialsId
                            uri = artifactoryUri
                        }

                        updateJiraOnMerge {
                            issueKey = changeset.jiraTicket
                            packageName = "JustSayingIflo"
                            version = packageVersion
                            credentialsId = jiraCredentialsId
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        deleteDir()
                    }

                    cleanupConsul {
                        repoName = changeset.repoName
                        prNumber = changeset.prNumber
                        consulBuildKey = changeset.consulBuildKey
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    deleteGithubBranch {
                        repoName = changeset.repoName
                        branchName = changeset.originatingBranch
                        logVerbose = verboseLogging
                    }
                }
            }
        }
    }
}