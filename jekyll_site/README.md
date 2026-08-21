# TownSuite FileSdConfigs — documentation site

A Jekyll documentation/landing site for **TownSuite.Prometheus.FileSdConfigs**,
implemented from the Claude Design handoff (`TownSuite FileSdConfigs.dc.html`).

It's a single scrolling docs page with a scroll-spy sidebar, platform tabs, and
copy-to-clipboard code blocks, themed with the TownSuite pinwheel palette
(deep blue `#00578E` primary, with the logo's orange/green/red accents).

Sections: Overview → Install → Configuration (`appsettings.json`) → Source JSON
(what the lookup endpoints must return) → Health endpoints (listing several
health endpoints for one site) → Failure handling (why an incomplete pass keeps
the previous file) → Run as a service → Prometheus.

## Structure

```
_config.yml              Site metadata, version, brand accent
_layouts/default.html    HTML shell — fonts, CSS, favicon, JS
index.html               The single docs page
_includes/               logo.html, icons.html, copy_btn.html
_data/                   Content for the repeated sections:
  nav.yml                Sidebar / scroll-spy items (also drives the scroll-spy in main.js)
  steps.yml              "How it works" chips
  outputs.yml            Output files
  settings_ref.yml       AppSettings reference table
  source_fields.yml      Settings (v1) vs SettingsV2 fields
  discovery_attributes.yml  Attributes recognized in the discover response
  extra_health_check.yml    ExtraHealthCheck lookup contract rows
  config_errors.yml         Behaviour on missing / invalid configuration
  pass_outcomes.yml         What gets written per pass outcome
  incomplete_causes.yml     What makes a pass incomplete
assets/css/style.css     All styling (CSS variables drive the theme)
assets/js/main.js        Scroll-spy, tabs, copy buttons
assets/img/              Logo / favicon
```

## Run locally

```bash
cd jekyll_site
bundle install
bundle exec jekyll serve
```

Then open http://localhost:4000.

## Keeping it in sync

The Source JSON and Health endpoints sections mirror the same material in the
repo root [`Readme.md`](../Readme.md); update both together. Adding a section
means adding an entry to `_data/nav.yml` and a matching `<section id="...">` in
`index.html` — the scroll-spy reads the sidebar links, so no JS change is needed.

## Re-theming

The accent colour is set once in `_config.yml` (`accent` / `accent_soft`) and
flows through CSS variables, so changing it re-themes the whole site. Update the
`version` field there to match the published release.
