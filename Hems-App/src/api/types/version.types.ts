export enum MobilePlatform {
  iOS = 1,
  Android = 2,
}

export interface AppVersionCheckRequest {
  platform: MobilePlatform;
  version: string;
}

export interface AppVersionCheckResponse {
  version: string;
  isUpdateRequired: boolean;
  isUpdateAvailable: boolean;
  storeUrl: string | null;
  description: string | null;
  revocationReason: string | null;
}
