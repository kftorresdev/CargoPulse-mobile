# ==============================================================================
# CargoPulse Mobile - ProGuard / R8 Shrinking & Obfuscation Rules
# ==============================================================================

# Preserve annotations and type signatures required for reflection / serialization
-keepattributes *Annotation*,Signature,InnerClasses,EnclosingMethod

# ------------------------------------------------------------------------------
# 1. CommunityToolkit.Maui
# ------------------------------------------------------------------------------
-keep class CommunityToolkit.Maui.** { *; }
-dontwarn CommunityToolkit.Maui.**
-keep class com.microsoft.maui.** { *; }
-dontwarn com.microsoft.maui.**

# ------------------------------------------------------------------------------
# 2. ZXing.Net.Maui (Barcode scanning and camera feeds)
# ------------------------------------------------------------------------------
-keep class ZXing.Net.Maui.** { *; }
-dontwarn ZXing.Net.Maui.**
-keep class com.google.zxing.** { *; }
-dontwarn com.google.zxing.**
-keep class com.journeyapps.barcodescanner.** { *; }
-dontwarn com.journeyapps.barcodescanner.**
-keep class androidx.camera.** { *; }
-dontwarn androidx.camera.**

# ------------------------------------------------------------------------------
# 3. SQLitePCLRaw & SQLite
# ------------------------------------------------------------------------------
-keep class SQLitePCL.** { *; }
-dontwarn SQLitePCL.**
-keep class sqlite3.** { *; }
-dontwarn sqlite3.**
-keep class org.sqlite.** { *; }
-dontwarn org.sqlite.**
-keep class net.sqlcipher.** { *; }
-dontwarn net.sqlcipher.**

# ------------------------------------------------------------------------------
# 4. System.Text.Json Serialized DTOs
# Preserve all DTO classes and their member fields/properties
# ------------------------------------------------------------------------------
-keep class CargoPulse.Mobile.Data.Dtos.** { *; }
-keepclassmembers class CargoPulse.Mobile.Data.Dtos.** { *; }
-keepclassmembers class * {
    @System.Text.Json.Serialization.* <fields>;
    @System.Text.Json.Serialization.* <methods>;
}
-dontwarn CargoPulse.Mobile.Data.Dtos.**

# ------------------------------------------------------------------------------
# 5. Local Database Models & Entities (SQLite-net-pcl)
# ------------------------------------------------------------------------------
-keep class CargoPulse.Mobile.Data.Local.Entities.** { *; }
-keepclassmembers class CargoPulse.Mobile.Data.Local.Entities.** { *; }
-dontwarn CargoPulse.Mobile.Data.Local.Entities.**
