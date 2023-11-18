import { type } from 'os'
import React from 'react'

type Props = {
    mediaFile: any
}

export default function MediaCard({mediaFile}: Props) {
  return (
    <a href='#'>
      <div className='w-full bg-gray-200 aspect-video rounded-lg overflow-hidden'>

      </div>
    </a>
  )
}
