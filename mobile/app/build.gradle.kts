// -----------------------------------------------------------------------------
// File        : app/build.gradle.kts
// Project     : VoltLink Mobile - Smart Solar Microgrid Trading System
// Description : Build configuration for the Android application. Reads the
//               Google Maps key from local.properties so it is never committed,
//               and exposes the Web API address as a build constant.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

import java.io.FileInputStream
import java.util.Properties

plugins {
    alias(libs.plugins.android.application)
}

// Load developer-specific values from untracked local.properties
val localProperties = Properties()
val localPropertiesFile = rootProject.file("local.properties")
if (localPropertiesFile.exists()) {
    FileInputStream(localPropertiesFile).use { localProperties.load(it) }
}

val mapsApiKey: String = localProperties.getProperty("MAPS_API_KEY") ?: ""
val apiBaseUrl: String = localProperties.getProperty("API_BASE_URL") ?: "http://10.0.2.2:8080/api/v1/"

android {
    namespace = "lk.sliit.voltlink"
    compileSdk = 36

    defaultConfig {
        applicationId = "lk.sliit.voltlink"
        minSdk = 26
        targetSdk = 36
        versionCode = 1
        versionName = "1.0"

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"

        // Injected into AndroidManifest.xml at build time
        manifestPlaceholders["MAPS_API_KEY"] = mapsApiKey

        // Accessible via BuildConfig.API_BASE_URL throughout the project
        buildConfigField("String", "API_BASE_URL", "\"$apiBaseUrl\"")
    }

    buildFeatures {
        viewBinding = true
        buildConfig = true
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
}

dependencies {
    // Core Android and Material Design components
    implementation(libs.androidx.activity.ktx)
    implementation(libs.androidx.appcompat)
    implementation(libs.androidx.constraintlayout)
    implementation(libs.androidx.core.ktx)
    implementation(libs.material)

    // Lists and pull-to-refresh
    implementation(libs.androidx.recyclerview)
    implementation(libs.androidx.swiperefreshlayout)

    // Lifecycle and Coroutines
    implementation(libs.androidx.lifecycle.runtime.ktx)
    implementation(libs.kotlinx.coroutines.android)

    // REST API & Networking
    implementation(libs.retrofit)
    implementation(libs.retrofit.gson)
    implementation(libs.okhttp.logging)

    // Google Maps & Location
    implementation(libs.play.services.maps)
    implementation(libs.play.services.location)

    // QR Code scanning & generation
    implementation(libs.zxing.embedded)
    implementation(libs.zxing.core)

    // Testing
    testImplementation(libs.junit)
    androidTestImplementation(libs.androidx.espresso.core)
    androidTestImplementation(libs.androidx.junit)
}