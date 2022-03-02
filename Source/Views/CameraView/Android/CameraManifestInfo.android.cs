using Android;
using Android.App;


//[assembly: UsesPermission(Manifest.Permission.Camera)]

// 09 Feb 2022
// Permission yg berhubungan dengan ExternalStorage harus ditambahkan secara manual via Android Project
// Contohnya Sa.To.Android, hrus tambahin ReadExternalStorage via AndroidManifest.xml or AssemblyInfo (C#)

//[assembly: UsesPermission(Manifest.Permission.ReadExternalStorage)]
//[assembly: UsesPermission(Manifest.Permission.WriteExternalStorage)]
//[assembly: UsesPermission(Manifest.Permission.ManageExternalStorage)]

[assembly: LinkerSafe] 