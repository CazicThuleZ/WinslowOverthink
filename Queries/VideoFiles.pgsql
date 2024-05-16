SELECT "Id", "FilePath", "FileName", "ShowTitle", "Description", "Duration", "Size", "ThumbnailUrl", "EpisodeTitle", "SeasonNumber", "EpisodeNumber", "FileCreateDateUTC", "DiskVolumeName", "YearReleased"
FROM "VideoFiles"
WHERE "ShowTitle" like '%Marvel%'
LIMIT 1000;


SELECT "Id", "FilePath", "FileName", "ShowTitle", "Description", "Duration", "Size", "ThumbnailUrl", "EpisodeTitle", "SeasonNumber", "EpisodeNumber", "FileCreateDateUTC", "DiskVolumeName", "YearReleased"
FROM "VideoFiles"
WHERE "ShowTitle" Ilike '%Evil%'
LIMIT 1000;

SELECT "Id", "FilePath", "FileName", "ShowTitle", "Description", "Duration", "Size", "ThumbnailUrl", "EpisodeTitle", "SeasonNumber", "EpisodeNumber", "FileCreateDateUTC", "DiskVolumeName", "YearReleased"
FROM "VideoFiles"
WHERE "ShowTitle" = 'Resident Alien'
LIMIT 1000;