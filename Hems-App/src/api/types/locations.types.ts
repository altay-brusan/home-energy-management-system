export interface CountryDto {
  id: string;
  name: string;
  code: string;
  isActive: boolean;
}

export interface CityDto {
  id: string;
  name: string;
  countryId: string;
  countryName: string;
  isActive: boolean;
}
