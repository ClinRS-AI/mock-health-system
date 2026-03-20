# Post-rename cleanup summary (for template repo’s TEMPLATE.md)

Use the text below in the **template project’s** TEMPLATE.md so people who copy the template know what to clean up once their repo is the real product, not a template.

---

## After you’ve migrated: cleaning up template references

Once you’ve run `./scripts/rename-project.sh YourProjectName` and this repo is your **actual project** (not a template), you can remove or adjust template-specific content so the repo reads as your product.

### 1. README.md

- **Opening description**  
  Change the first line from “X is a **full-stack template** for new projects” to a short description of your real product (e.g. “Digital Toolkit is a …” or “MyApp is a …”).

- **Template callout**  
  Remove the block that says **“Using this as a template?”** (the one that tells users to run `rename-project.sh` and points to TEMPLATE.md). That callout is only for the template repo.

- **Closing line**  
  Update the line about opening the app in the browser (e.g. “see the X UI”) so it uses your product name and no longer says “template” or “baseline.”

### 2. TEMPLATE.md

- **In the new (forked) project**  
  Either delete TEMPLATE.md (it’s only needed in the template repo), or replace it with a one-line note for history, e.g. “This project was created from the [Template Name] template.”

- **In the template repo**  
  Keep TEMPLATE.md as the instructions for people who “Use this template.” You can add this “After you’ve migrated” section there so they know what to clean up.

### 3. scripts/rename-project.sh

- **In your real project**  
  Optional: keep it if you might reuse it (e.g. to spin off another app from this one); otherwise you can remove it.

- **In the template repo**  
  Keep it; new users need it to rename the project.

### 4. This file (docs/template-cleanup-summary.md)

- **In your real project**  
  Remove this file once you've finished the cleanup above. It was only a checklist for migrating from the template.

### Summary checklist (for the new project)

- [ ] README: product description, no “template” framing, no “Using this as a template?” callout.
- [ ] README: “see the X UI” (or similar) uses your product name.
- [ ] TEMPLATE.md: removed or replaced with a short “created from template” note.
- [ ] Optional: remove `scripts/rename-project.sh` if you won’t use it again.
- [ ] Remove this file: `docs/template-cleanup-summary.md`.
