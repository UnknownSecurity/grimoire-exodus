export default {
  extends: ["@commitlint/config-conventional"],
  rules: {
    "body-leading-blank": [1, "always"],
    "body-max-line-length": [1, "always", 100],
    "footer-leading-blank": [1, "always"],
    "footer-max-line-length": [1, "always", 100]
  }
};
