using System;
using System.IO;

namespace SimpleLoginApp.Services;

public static class CachePaths
{
    private static readonly string BasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SimpleLoginApp",
        "cache");

    private static readonly string ClassPath = Path.Combine(BasePath, "class");
    private static readonly string CoursePath = Path.Combine(BasePath, "course");
    private static readonly string LecturePath = Path.Combine(BasePath, "lecture");

    public static string GetClassImagePath(string classId)
    {
        Directory.CreateDirectory(ClassPath);
        return Path.Combine(ClassPath, classId);
    }

    public static string GetCourseImagePath(string courseId)
    {
        Directory.CreateDirectory(CoursePath);
        return Path.Combine(CoursePath, courseId);
    }

    public static string GetLecturePath(string lectureId)
    {
        Directory.CreateDirectory(LecturePath);
        return Path.Combine(LecturePath, lectureId);
    }
}
