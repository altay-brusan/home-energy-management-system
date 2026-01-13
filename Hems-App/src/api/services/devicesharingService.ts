import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  CreateInvitationCommand,
  InvitationListDto,
} from '../types/deviceSharingService.ts';
import { AcceptInvitationCommand } from '../types/invitations.types';

export const invitationsService = {
  async createInvitation(data: any): Promise<void> {
    const response = await apiClient.post(ENDPOINTS.INVITATIONS.CREATE, data);
    return response.data;
  },

  async getInvitationsBySite(siteId: string): Promise<InvitationListDto> {
    const url = ENDPOINTS.INVITATIONS.GET_BY_SITE.replace(':siteId', siteId);
    const response = await apiClient.get<InvitationListDto>(url);
    return response.data;
  },

  async acceptInvitation(data: AcceptInvitationCommand): Promise<void> {
    const response = await apiClient.post(ENDPOINTS.INVITATIONS.ACCEPT, data);
    return response.data;
  },

  async declineInvitation(data: { token: string }): Promise<void> {
    const response = await apiClient.post(ENDPOINTS.INVITATIONS.DECLINE, data);
    return response.data;
  },

  async deleteInvitation(invitationId: string): Promise<void> {
    const url = ENDPOINTS.INVITATIONS.DELETE.replace(':invitationId', invitationId);
    await apiClient.delete(url);
  },

  async removeUser(id: string): Promise<void> {
    const url = `${ENDPOINTS.INVITATIONS.REMOVE_USER}/${id}`;
    await apiClient.delete(url);
  },

};
