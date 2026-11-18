from pathlib import Path
here = Path(__file__).parent
page = (here / "page_template.html").read_text().replace("/*__ROUTINES__*/", (here / "routines.js").read_text())
(here / "preview").mkdir(exist_ok=True)
(here / "preview" / "index.html").write_text(page)
print("wrote preview/index.html", len(page))
