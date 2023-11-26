import Image from 'next/image'
import { type } from 'os'
import React from 'react'
import ThumbnailImage from './ThumbnailImage'
import { MediaFile } from '../types/Index'

type Props = {
  mediaFile: MediaFile
}

export default function MediaCard({ mediaFile }: Props) {
  return (
    <a href='#' className='group'>
      <div className='w-full bg-gray-200 aspect-w-16 aspect-h-10 rounded-lg overflow-hidden'>
        <div>
           <ThumbnailImage thumbnailUrl={mediaFile.thumbnailUrl} showTitle={mediaFile.showTitle} />
          <div className='absolute bottom-2 left-2'>
              <p className='font-semibold text-sm'>{mediaFile.showTitle}</p>  
          </div>
        </div>
      </div>
      <div className='flex justify-between items-center mt-4'>
          <p className='font-semibold text-sm'>{mediaFile.showTitle}</p>
          <p className='font-semibold text-sm'>{mediaFile.seasonNumber} : {mediaFile.episodeNumber}</p>
        </div>
    </a>
  )
}
