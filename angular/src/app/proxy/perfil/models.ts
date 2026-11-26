
export interface ActualizarFotoDto {
  fotoBase64: string;
}

export interface ActualizarPerfilDto {
  name: string;
  surname?: string;
  email: string;
  phoneNumber?: string | null;
}

export interface PerfilDto {
  id?: string;
  userName?: string;
  name?: string;
  surname?: string;
  email?: string;
  phoneNumber?: string | null;
  fotoPerfil?: string | null;
}
