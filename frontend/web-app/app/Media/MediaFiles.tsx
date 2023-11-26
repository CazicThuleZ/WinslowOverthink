import React from 'react'
import MediaCard from './MediaCard';
import { MediaFile, PagedResult } from '../types/Index';
import AppPagination from '../components/AppPagination';

async function getData(): Promise<PagedResult<MediaFile>> {
  const res = await fetch('http://localhost:7002/api/Search?pageSize=2')

  if (!res.ok) {
    throw new Error(res.statusText)
  }

  return res.json();
}

export default async function MediaFiles() {
  const data = await getData();

  return (

    <>
      <div>

        <div className='grid grid-cols-4 gap-6'>
          {data && data.result.map((mediaFile: any) => (
            <MediaCard mediaFile={mediaFile} key={mediaFile.id} />
          ))}
        </div>

        <div className='flex justify-center mt-4'>
          <AppPagination  currentPage={1} pageCount={data.pageCount} />
        </div>

        {/* {JSON.stringify(data, null, 2)} */}
      </div>
    </>
  )
}
