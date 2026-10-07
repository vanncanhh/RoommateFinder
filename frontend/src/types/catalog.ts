// backend/src/RoommateFinder.Application/Dtos/CatalogDtos.cs
import type { LandmarkType, ReasonAppliesTo } from './enums'

export interface AreaDto {
  areaId: number
  name: string
  district: string | null
  city: string
  latitude: number | null
  longitude: number | null
  isActive: boolean
}

export interface LandmarkDto {
  landmarkId: number
  name: string
  type: LandmarkType
  areaId: number
  areaName: string
  address: string | null
  latitude: number
  longitude: number
  isActive: boolean
}

export interface AmenityDto {
  amenityId: number
  name: string
  isActive: boolean
}

export interface ReportReasonDto {
  reasonId: number
  name: string
  appliesTo: ReasonAppliesTo
  isActive: boolean
}

export interface AreaUpsertRequest {
  name: string
  district?: string | null
  city: string
  latitude?: number | null
  longitude?: number | null
  isActive: boolean
}

export interface LandmarkUpsertRequest {
  name: string
  type: LandmarkType
  areaId: number
  address?: string | null
  latitude: number
  longitude: number
  isActive: boolean
}

export interface AmenityUpsertRequest {
  name: string
  isActive: boolean
}

export interface ReportReasonUpsertRequest {
  name: string
  appliesTo: ReasonAppliesTo
  isActive: boolean
}
