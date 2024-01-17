'use server'

import { MediaFile, PagedResult } from "../types/Index";

export async function getData(query: string): Promise<PagedResult<MediaFile>> {
    const res = await fetch(`http://localhost:7002/api/Search${query}`)
  
    if (!res.ok) {
      throw new Error(res.statusText)
    }
  
    return res.json();
  }