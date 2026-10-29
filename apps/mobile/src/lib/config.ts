function required(name: string, value: string | undefined): string {
  if (!value) {
    throw new Error(`${name} is not set. Copy .env.example to .env and fill it in.`);
  }

  return value;
}

// Expo only replaces process.env.EXPO_PUBLIC_* when the name is written out in full,
// so each one appears literally here. Reading process.env[name] would give undefined.
export const config = {
  apiUrl: required("EXPO_PUBLIC_API_URL", process.env.EXPO_PUBLIC_API_URL),
  auth0Domain: required("EXPO_PUBLIC_AUTH0_DOMAIN", process.env.EXPO_PUBLIC_AUTH0_DOMAIN),
  auth0ClientId: required("EXPO_PUBLIC_AUTH0_CLIENT_ID", process.env.EXPO_PUBLIC_AUTH0_CLIENT_ID),
  auth0Audience: required("EXPO_PUBLIC_AUTH0_AUDIENCE", process.env.EXPO_PUBLIC_AUTH0_AUDIENCE),
  // Must match customScheme in app.config.ts and the callback URLs in Auth0.
  authScheme: "tallixauth",
};