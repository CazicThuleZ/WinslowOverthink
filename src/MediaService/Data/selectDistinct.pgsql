

SELECT "DiskVolumeName", "ShowTitle", "SeasonNumber", "EpisodeNumber", COUNT(*)
FROM "VideoFiles"
Where "EpisodeNumber" > 0
GROUP BY "DiskVolumeName","ShowTitle", "SeasonNumber", "EpisodeNumber"
HAVING COUNT(*) > 1
ORDER BY "ShowTitle";


SELECT *
FROM "VideoFiles"
Where "ShowTitle" = 'Bridge and Tunnel'