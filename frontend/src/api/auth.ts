import type {
  AuthResponse,
  ChangePasswordRequest,
  CurrentUserDto,
  ForgotPasswordRequest,
  LoginRequest,
  MessageResponse,
  RegisterRequest,
  ResetPasswordRequest,
} from '../types'
import { apiClient } from './client'

const BASE = '/api/auth'

export const authApi = {
  register: async (body: RegisterRequest): Promise<MessageResponse> =>
    (await apiClient.post<MessageResponse>(`${BASE}/register`, body)).data,

  login: async (body: LoginRequest): Promise<AuthResponse> =>
    (await apiClient.post<AuthResponse>(`${BASE}/login`, body)).data,

  forgotPassword: async (body: ForgotPasswordRequest): Promise<MessageResponse> =>
    (await apiClient.post<MessageResponse>(`${BASE}/forgot-password`, body)).data,

  resetPassword: async (body: ResetPasswordRequest): Promise<MessageResponse> =>
    (await apiClient.post<MessageResponse>(`${BASE}/reset-password`, body)).data,

  changePassword: async (body: ChangePasswordRequest): Promise<MessageResponse> =>
    (await apiClient.put<MessageResponse>(`${BASE}/change-password`, body)).data,

  me: async (): Promise<CurrentUserDto> => (await apiClient.get<CurrentUserDto>(`${BASE}/me`)).data,
}
