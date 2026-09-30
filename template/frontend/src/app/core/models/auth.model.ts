export interface LoginInput {
  email: string;
  password: string;
}

export interface AuthResult {
  token: string;
  email: string;
  name: string;
  role: string;
}
