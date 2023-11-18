import React from 'react'
import MediaCard from './MediaCard';

async function getData() {
  const res = await fetch('http://localhost:7002/api/search?pageSize=10')

  if (!res.ok) {
    throw new Error(res.statusText)
  }

  return res.json();
}

export default async function MediaFiles() {
  const data = await getData();
  return (
    <div>

      <div className='grid grid-cols-4 gap-6'>
        {data && data.result.map((mediaFile: any) => (
          <MediaCard mediaFile={mediaFile} key={mediaFile.id} />
        ))}
      </div>

      {/* {JSON.stringify(data, null, 2)} */}
    </div>
  )
}
