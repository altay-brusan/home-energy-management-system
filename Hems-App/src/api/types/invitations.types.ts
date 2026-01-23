// Invitations API Types

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
  status: InvitationStatus;
  acceptanceTime?: string | null;
  created: string;
}

export enum InvitationStatus {
  Pending = 1,
  Accepted = 2,
  Declined = 3,
  Cancelled = 4,
}

export interface InvitationsVm {
  withUser: InvitationDto[];
  withoutUser: InvitationDto[];
}

export interface AcceptInvitationCommand {
  token: string;
}

export interface DeclineInvitationCommand {
  token: string;
}

export interface UserHomeSitesVm {
  myHomes: UserSiteDto[];
  sharedHomes: UserSiteDto[];
}

export interface UserSiteDto {
  id: string;
  userId: string;
  siteId: string;
  siteName: string;
  role: UserSiteRole;
}

export enum UserSiteRole {
  Owner = 1,
  Guest = 2,
}

export interface NotificationDto {
  id: string;
  message: string;
  isRead: boolean;
  invitationId: string;
  created: string;
}

export interface GetInvitationsBySiteParams {
  siteId: string;
}

export interface PendingInvitationDto {
  invitationId: string;
  siteName: string;
  invitedByEmail: string;
  token: string;
  createdAt: string;
  isExpired: boolean;
}

export interface PendingInvitationsResult {
  hasPendingInvitations: boolean;
  invitations: PendingInvitationDto[];
}
