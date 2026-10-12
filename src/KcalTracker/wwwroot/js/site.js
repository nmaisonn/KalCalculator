document.addEventListener("DOMContentLoaded", () => {
  document.querySelectorAll("[data-orden-ej]").forEach(iniciarLista);
  envolverFechas(document);
});
document.addEventListener("click", (e) => {
  document.querySelectorAll("details.menu[open]").forEach((d) => {
    if (!d.contains(e.target)) d.removeAttribute("open");
  });
  const cal = e.target.closest(".nav-cal-btn");
  if (cal) {
    const input = cal.querySelector("input[type=date]");
    if (input && typeof input.showPicker === "function") {
      e.preventDefault();
      try { input.showPicker(); } catch { /* el overlay nativo sigue siendo el respaldo */ }
      return;
    }
  }
  const abrir = e.target.closest("[data-abrir-puntual]");
  if (abrir) {
    e.preventDefault();
    abrirModalPuntual(abrir);
    return;
  }
  if (e.target.closest("[data-cerrar-puntual]")) {
    document.getElementById("modal-puntual")?.close();
    return;
  }
  const modal = document.getElementById("modal-puntual");
  if (modal?.open && e.target === modal) modal.close();
});
document.addEventListener("keydown", (e) => {
  if (e.key !== "Escape") return;
  document.querySelectorAll("details.menu[open]").forEach((d) => d.removeAttribute("open"));
});
document.addEventListener("submit", (e) => {
  const form = e.target;
  if (form && form.id === "form-puntual") {
    e.preventDefault();
    enviarPuntual();
    return;
  }
  if (!form || !form.querySelectorAll) return;
  form.querySelectorAll(".campo-fecha").forEach((wrap) => {
    const vista = wrap.querySelector(".fecha-vista");
    const nativa = wrap.querySelector(".fecha-nativa");
    if (!vista || !nativa) return;
    if (!vista.value.trim()) nativa.value = "";
    else {
      const iso = vistaAIso(vista.value);
      if (iso) nativa.value = iso;
    }
  });
}, true);
document.body.addEventListener("htmx:afterSwap", (e) => {
  const el = e.detail?.elt || e.target;
  if (el && el.querySelectorAll) envolverFechas(el);
  if (el && (el.id === "hoy-comidas" || el.id === "entreno" || el.querySelector?.("#hoy-comidas") || el.querySelector?.("#entreno")))
    document.getElementById("modal-puntual")?.close();
});

function abrirModalPuntual(btn) {
  const d = document.getElementById("modal-puntual");
  const form = document.getElementById("form-puntual");
  if (!d || !form) return;
  const handler = btn.dataset.handler || "";
  const url = `?handler=${encodeURIComponent(handler)}`;
  form.setAttribute("action", url);
  form.setAttribute("data-post", url);
  form.setAttribute("data-target", btn.dataset.hxTarget || "#hoy-comidas");
  form.setAttribute("hx-params", "Fecha,tipoComidaId,nombre,kcal");
  const slot = btn.closest(".slot-carga")?.querySelector("select[name=Eleccion]");
  if (slot) slot.value = "";
  form.querySelector("[name=tipoComidaId]").value = btn.dataset.tipo || "";
  form.querySelector("#modal-puntual-titulo").textContent = btn.dataset.titulo || "Agregar";
  form.querySelector("#modal-puntual-ayuda").textContent =
    btn.dataset.ayuda || "No se guarda en el catálogo. Solo entra en este día.";
  form.querySelector("[name=nombre]").value = "";
  form.querySelector("[name=kcal]").value = "";
  d.showModal();
  form.querySelector("[name=nombre]").focus();
}

let puntualEnVuelo = false;

function enviarPuntual() {
  const form = document.getElementById("form-puntual");
  const modal = document.getElementById("modal-puntual");
  if (!form || puntualEnVuelo) return;
  if (typeof form.reportValidity === "function" && !form.reportValidity()) return;
  const url = form.getAttribute("data-post") || form.getAttribute("action");
  const target = form.getAttribute("data-target") || "#hoy-comidas";
  if (!url) return;
  puntualEnVuelo = true;
  const boton = form.querySelector("[type=submit]");
  if (boton) boton.disabled = true;
  const listo = () => {
    puntualEnVuelo = false;
    if (boton) boton.disabled = false;
  };
  if (window.htmx) {
    const valores = {};
    new FormData(form).forEach((v, k) => { valores[k] = v; });
    Promise.resolve(htmx.ajax("POST", url, {
      source: form,
      target,
      swap: "outerHTML show:none focus-scroll:false",
      values: valores,
    })).then(() => {
      const nom = form.querySelector("[name=nombre]");
      const kcal = form.querySelector("[name=kcal]");
      if (nom) nom.value = "";
      if (kcal) kcal.value = "";
      modal?.close();
    }).catch(() => {}).finally(listo);
  } else {
    form.submit();
  }
}
document.addEventListener("keydown", (e) => {
  if (e.key !== "Enter") return;
  const campo = e.target;
  if (!(campo instanceof HTMLInputElement) && !(campo instanceof HTMLSelectElement)) return;
  if (campo.type === "submit" || campo.type === "button") return;
  const puntual = campo.closest("#form-puntual");
  if (puntual && campo.name === "nombre") {
    e.preventDefault();
    puntual.querySelector("[name=kcal]")?.focus();
    return;
  }
  const form = campo.closest("form[data-live-calc]");
  if (!form) return;
  e.preventDefault();
  campo.dispatchEvent(new Event("change", { bubbles: true }));
});

function tokenAntiforgery() {
  const bruto = document.body.getAttribute("hx-headers");
  if (!bruto) return "";
  try {
    return JSON.parse(bruto).RequestVerificationToken || "";
  } catch {
    return "";
  }
}

function iniciarLista(lista) {
  let arrastrado = null;

  lista.querySelectorAll(":scope > li").forEach((li) => {
    const asa = li.querySelector(".asa");
    if (asa) {
      asa.addEventListener("dragstart", (e) => {
        arrastrado = li;
        li.classList.add("arrastrando");
        e.dataTransfer.effectAllowed = "move";
        e.dataTransfer.setData("text/plain", li.dataset.id || "");
        if (typeof e.dataTransfer.setDragImage === "function")
          e.dataTransfer.setDragImage(li, 24, 24);
      });
      asa.addEventListener("dragend", () => {
        li.classList.remove("arrastrando");
        lista.querySelectorAll(".soltar-antes, .soltar-despues").forEach((x) => {
          x.classList.remove("soltar-antes", "soltar-despues");
        });
        arrastrado = null;
      });
    }

    li.addEventListener("dragover", (e) => {
      e.preventDefault();
      const sobre = e.currentTarget;
      if (!arrastrado || sobre === arrastrado) return;
      const rect = sobre.getBoundingClientRect();
      const antes = e.clientY < rect.top + rect.height / 2;
      sobre.classList.toggle("soltar-antes", antes);
      sobre.classList.toggle("soltar-despues", !antes);
    });
    li.addEventListener("dragleave", (e) => {
      e.currentTarget.classList.remove("soltar-antes", "soltar-despues");
    });
    li.addEventListener("drop", (e) => {
      e.preventDefault();
      const sobre = e.currentTarget;
      sobre.classList.remove("soltar-antes", "soltar-despues");
      if (!arrastrado || sobre === arrastrado) return;
      const rect = sobre.getBoundingClientRect();
      if (e.clientY < rect.top + rect.height / 2) lista.insertBefore(arrastrado, sobre);
      else lista.insertBefore(arrastrado, sobre.nextSibling);
      guardarOrden(lista);
    });

    li.querySelector(".subir")?.addEventListener("click", () => {
      const prev = li.previousElementSibling;
      if (prev) {
        lista.insertBefore(li, prev);
        guardarOrden(lista);
      }
    });
    li.querySelector(".bajar")?.addEventListener("click", () => {
      const next = li.nextElementSibling;
      if (next) {
        lista.insertBefore(next, li);
        guardarOrden(lista);
      }
    });
  });
}

function guardarOrden(lista) {
  const ids = [...lista.querySelectorAll(":scope > li[data-id]")].map((li) => li.dataset.id);
  const cuerpo = new URLSearchParams();
  ids.forEach((id) => cuerpo.append("ids", id));
  const token = tokenAntiforgery();
  if (token) cuerpo.append("__RequestVerificationToken", token);

  const rutina = lista.dataset.rutina;
  const dia = lista.dataset.dia;
  fetch(`?handler=ReordenarEjercicios&id=${encodeURIComponent(rutina)}&diaId=${encodeURIComponent(dia)}`, {
    method: "POST",
    headers: {
      RequestVerificationToken: token,
      "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8",
    },
    body: cuerpo.toString(),
  }).then((r) => {
    if (!r.ok) window.location.reload();
  }).catch(() => window.location.reload());
}

function isoAVista(iso) {
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso || "");
  return m ? `${m[3]}/${m[2]}/${m[1]}` : "";
}

function vistaAIso(vista) {
  const m = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/.exec((vista || "").trim());
  if (!m) return "";
  const d = +m[1], mes = +m[2], anio = +m[3];
  const dt = new Date(anio, mes - 1, d);
  if (dt.getFullYear() !== anio || dt.getMonth() !== mes - 1 || dt.getDate() !== d) return "";
  return `${anio}-${String(mes).padStart(2, "0")}-${String(d).padStart(2, "0")}`;
}

function envolverFechas(raiz) {
  (raiz.querySelectorAll ? raiz : document).querySelectorAll("input[type='date']").forEach(envolverFecha);
}

function envolverFecha(nativa) {
  if (nativa.closest(".campo-fecha") || nativa.dataset.sinEnvolver != null || nativa.closest(".nav-cal")) return;
  nativa.lang = "es-UY";
  const wrap = document.createElement("span");
  wrap.className = "campo-fecha";
  if (nativa.classList.contains("corto")) wrap.classList.add("corto");
  nativa.parentNode.insertBefore(wrap, nativa);

  const vista = document.createElement("input");
  vista.type = "text";
  vista.className = "fecha-vista";
  vista.placeholder = "dd/mm/aaaa";
  vista.autocomplete = "off";
  vista.inputMode = "numeric";
  vista.maxLength = 10;
  vista.spellcheck = false;
  vista.value = isoAVista(nativa.value);
  vista.setAttribute("aria-label", nativa.getAttribute("aria-label") || "Fecha (dd/mm/aaaa)");
  if (nativa.id) {
    vista.id = nativa.id;
    nativa.removeAttribute("id");
  }
  if (nativa.disabled) vista.disabled = true;
  if (nativa.readOnly) vista.readOnly = true;

  nativa.classList.add("fecha-nativa");
  nativa.setAttribute("aria-hidden", "true");
  nativa.tabIndex = -1;
  wrap.appendChild(vista);
  wrap.appendChild(nativa);

  vista.addEventListener("input", () => {
    let d = vista.value.replace(/[^\d]/g, "").slice(0, 8);
    if (d.length > 4) vista.value = `${d.slice(0, 2)}/${d.slice(2, 4)}/${d.slice(4)}`;
    else if (d.length > 2) vista.value = `${d.slice(0, 2)}/${d.slice(2)}`;
    else vista.value = d;
    if (!vista.value.trim()) {
      nativa.value = "";
      return;
    }
    const iso = vistaAIso(vista.value);
    if (iso && (!nativa.min || iso >= nativa.min) && (!nativa.max || iso <= nativa.max))
      nativa.value = iso;
  });
  vista.addEventListener("change", () => {
    if (!vista.value.trim()) {
      nativa.value = "";
      nativa.dispatchEvent(new Event("change", { bubbles: true }));
      return;
    }
    const iso = vistaAIso(vista.value);
    if (iso && (!nativa.min || iso >= nativa.min) && (!nativa.max || iso <= nativa.max)) {
      nativa.value = iso;
      nativa.dispatchEvent(new Event("change", { bubbles: true }));
    } else {
      vista.value = isoAVista(nativa.value);
    }
  });
  nativa.addEventListener("input", () => { vista.value = isoAVista(nativa.value); });
  nativa.addEventListener("change", () => { vista.value = isoAVista(nativa.value); });
}

document.addEventListener("pointerover", (e) => {
  const g = e.target.closest(".punto-g");
  const box = g?.closest("[data-graf]");
  if (g && box) mostrarGrafTip(box, g);
});
document.addEventListener("pointerout", (e) => {
  const box = e.target.closest("[data-graf]");
  if (!box) return;
  const to = e.relatedTarget;
  if (to && box.contains(to) && to.closest?.(".punto-g")) return;
  ocultarGrafTip(box);
});
document.addEventListener("focusin", (e) => {
  const g = e.target.closest(".punto-g");
  const box = g?.closest("[data-graf]");
  if (g && box) mostrarGrafTip(box, g);
});
document.addEventListener("focusout", (e) => {
  const box = e.target.closest("[data-graf]");
  if (!box) return;
  const to = e.relatedTarget;
  if (to && box.contains(to) && to.closest?.(".punto-g")) return;
  ocultarGrafTip(box);
});

function mostrarGrafTip(box, g) {
  box.querySelectorAll(".punto-g.on").forEach((x) => x.classList.remove("on"));
  g.classList.add("on");
  const tip = box.querySelector(".graf-tip");
  if (!tip) return;
  const fecha = g.dataset.fecha || "";
  const valor = g.dataset.valor || "";
  tip.innerHTML = `<span>${fecha}</span><strong>${valor}</strong>`;
  tip.hidden = false;
  tip.classList.remove("abajo");
  const br = box.getBoundingClientRect();
  const pr = (g.querySelector(".punto") || g).getBoundingClientRect();
  const left = pr.left - br.left + pr.width / 2;
  const top = pr.top - br.top;
  tip.style.left = `${left}px`;
  tip.style.top = `${top}px`;
  if (top < tip.offsetHeight + 12) tip.classList.add("abajo");
}

function ocultarGrafTip(box) {
  box.querySelectorAll(".punto-g.on").forEach((x) => x.classList.remove("on"));
  const tip = box.querySelector(".graf-tip");
  if (tip) {
    tip.hidden = true;
    tip.classList.remove("abajo");
  }
}
