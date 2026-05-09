import { UsersManagementService } from 'archon-ui'
import type { CommercialResponsible } from '../types/commercialResponsible'

const RESPONSIBLE_ROLE = 'Comercial'

export const commercialResponsibleService = {
  async getAll(): Promise<CommercialResponsible[]> {
    const users = await UsersManagementService.listInCurrentContract()
    return users
      .filter((user) => user.isActive && user.roleName === RESPONSIBLE_ROLE)
      .map((user) => ({
        id: user.userId,
        userId: user.userId,
        name: user.name,
        email: user.email,
        isActive: user.isActive,
      }))
  },
}
