export interface CreateInvitationCommand {
  email: string;
  siteId: string;
}
export interface InvitationDto {
  id: string;
  email: string;
  invitedUserId?: string | null;
  invitedByUserId: string;
  invitedByUserName: string;
  siteId: string;
  siteName: string;
  token: string;
  status: number;
  acceptanceTime?: string | null;
  created: string;
}

export interface InvitationListDto {
  withUser: InvitationDto[];
  withoutUser: InvitationDto[];
}
