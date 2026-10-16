pluginManagement {
    repositories {
        google()
        mavenCentral()
        gradlePluginPortal()
    }
}

dependencyResolutionManagement {
    repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
    repositories {
        google()
        mavenCentral()
    }
}

rootProject.name = "GrimoireExodusMobile"

include(":app")
include(":core:designsystem")
include(":core:domain")
include(":core:legacy-gbot")
include(":core:model")
