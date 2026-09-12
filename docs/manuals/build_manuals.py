#!/usr/bin/env python3
"""Build the seven Internet Radio LS user-manual PDFs from Markdown sources."""

from pathlib import Path
import shutil
import subprocess
import tempfile

from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH

ROOT = Path(__file__).resolve().parent
SOURCE_DIR = ROOT / "source"
OUTPUT_DIR = ROOT / "pdf"
DOCX_DIR = ROOT / ".build_docx"
LANGUAGES = ("DE", "EN", "ES", "FR", "IT", "PT-BR", "TR")


def make_reference_docx(path: Path) -> None:
    doc = Document()
    section = doc.sections[0]
    section.page_width = Inches(8.5)
    section.page_height = Inches(11)
    margin = Inches(0.7083333333)  # about 18 mm
    section.top_margin = margin
    section.bottom_margin = margin
    section.left_margin = margin
    section.right_margin = margin

    def configure_style(name, size, bold=None, color=None, before=None, after=None):
        style = doc.styles[name]
        style.font.name = "Liberation Sans"
        style.font.size = Pt(size)
        if bold is not None:
            style.font.bold = bold
        if color is not None:
            style.font.color.rgb = RGBColor(*color)
        if before is not None:
            style.paragraph_format.space_before = Pt(before)
        if after is not None:
            style.paragraph_format.space_after = Pt(after)

    configure_style("Normal", 10, after=5)
    configure_style("Title", 24, True, (20, 62, 100), after=8)
    configure_style("Heading 1", 16, True, (20, 62, 100), before=10, after=4)
    configure_style("Heading 2", 12.5, True, (36, 93, 145), before=10, after=4)
    configure_style("Heading 3", 11, True, (60, 60, 60), before=10, after=4)
    if "Table Grid" in doc.styles:
        configure_style("Table Grid", 9, after=0)

    footer = section.footer.paragraphs[0]
    footer.text = "LOS SANTOS INTERNET RADIO LS - v1.0.2 BETA TEST"
    footer.alignment = WD_ALIGN_PARAGRAPH.CENTER
    for run in footer.runs:
        run.font.name = "Liberation Sans"
        run.font.size = Pt(8)
        run.font.color.rgb = RGBColor(90, 90, 90)

    doc.save(path)


def run(*args) -> None:
    print("+", " ".join(str(arg) for arg in args), flush=True)
    subprocess.run([str(arg) for arg in args], check=True)


def main() -> None:
    for command in ("pandoc", "libreoffice"):
        if shutil.which(command) is None:
            raise SystemExit(f"Missing required command: {command}")

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    DOCX_DIR.mkdir(parents=True, exist_ok=True)

    with tempfile.TemporaryDirectory() as temp_dir:
        reference = Path(temp_dir) / "reference.docx"
        make_reference_docx(reference)

        for language in LANGUAGES:
            source = SOURCE_DIR / f"{language}.md"
            if not source.is_file():
                raise SystemExit(f"Missing source manual: {source}")
            docx = DOCX_DIR / f"{language}.docx"
            run(
                "pandoc",
                source,
                "--from=gfm",
                "--to=docx",
                f"--reference-doc={reference}",
                "-o",
                docx,
            )

        run(
            "libreoffice",
            "--headless",
            "--convert-to",
            "pdf",
            "--outdir",
            OUTPUT_DIR,
            *[DOCX_DIR / f"{language}.docx" for language in LANGUAGES],
        )

    for language in LANGUAGES:
        generated = OUTPUT_DIR / f"{language}.pdf"
        final = OUTPUT_DIR / f"Internet_Radio_LS_v1.0.2_BETA_Manual_{language}.pdf"
        if not generated.is_file():
            raise SystemExit(f"PDF was not generated: {generated}")
        generated.replace(final)

    shutil.rmtree(DOCX_DIR, ignore_errors=True)
    print("Built seven PDF manuals successfully.")


if __name__ == "__main__":
    main()
