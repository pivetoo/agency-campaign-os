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
}
