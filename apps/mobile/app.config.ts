import type { ConfigContext, ExpoConfig } from "expo/config";

// Plugins this file configures itself. A bare entry for either in app.json
// (`npx expo install` can add one automatically) is dropped, so each runs
// exactly once, with its options.
const MANAGED_PLUGINS = ["react-native-auth0", "expo-build-properties"];

function pluginName(plugin: unknown): unknown {
  return Array.isArray(plugin) ? plugin[0] : plugin;
}

export default ({ config }: ConfigContext): ExpoConfig => {
  const domain = process.env.EXPO_PUBLIC_AUTH0_DOMAIN;

  if (!domain) {
    throw new Error("EXPO_PUBLIC_AUTH0_DOMAIN is not set. Copy .env.example to .env and fill it in.");
  }

  const otherPlugins = (config.plugins ?? []).filter(
    (plugin) => !MANAGED_PLUGINS.includes(pluginName(plugin) as string),
  );

  return {
    ...config,
    name: config.name ?? "Tallix",
    slug: config.slug ?? "tallix",
    plugins: [
      ...otherPlugins,
      [
        "react-native-auth0",
        {
          domain,
          // Must match authScheme in lib/config.ts and the callback URLs in Auth0.
          customScheme: "tallixauth",
        },
      ],
      [
        "expo-build-properties",
        {
          android: {
            // The Auth0 Android SDK needs Android 8.0 (API 26) or newer.
            minSdkVersion: 26,
          },
        },
      ],
    ],
  };
};