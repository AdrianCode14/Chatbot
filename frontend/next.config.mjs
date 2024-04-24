export default {
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: "http://localhost:5179/api/:path*", //Restrictions CORS, si tu comprend pas dmd moi
      },
    ];
  },
};
