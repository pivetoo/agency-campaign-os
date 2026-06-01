import { httpClient } from 'archon-ui'

export const profileApiService = {
  async uploadAvatar(file: File): Promise<string> {
    const formData = new FormData()
    formData.append('file', file)
    const response = await httpClient.post<{ url: string }>('/Profile/UploadAvatar', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return response.data!.url
  },
  async removeAvatar(): Promise<void> {
    await httpClient.delete('/Profile/RemoveAvatar')
  },
  async updateProfile(payload: { name: string }): Promise<{ name?: string; username?: string; email?: string; avatarUrl?: string }> {
    const response = await httpClient.put<{ name?: string; username?: string; email?: string; avatarUrl?: string }>('/Profile/UpdateProfile', payload)
    return response.data ?? {}
  },
}
