export type PagedResult<T> = {
    result: T[]
    pageCount: number
    totalCount: number
}

export type MediaFile = {
    diskVolumeName: string
    filePath: string
    fileName: string
    showTitle: string
    description: string
    yearReleased: any
    duration: number
    size: number
    thumbnailUrl: string
    episodeTitle: string
    seasonNumber: number
    episodeNumber: number
    fileCreateDateUTC: string
    id: string
  }
  