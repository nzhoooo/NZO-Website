document.addEventListener("DOMContentLoaded", () => {
  const heroSlides = [...document.querySelectorAll(".hero__slide")];
  const topNav = document.getElementById("top-nav");
  const themeToggle = document.getElementById("theme-toggle");
  const motionQuery = window.matchMedia("(prefers-reduced-motion: reduce)");
  let activeHeroSlide = 0;
  const dockLinks = [...document.querySelectorAll(".dock__item")];
  const dockTargets = dockLinks
    .map((link) => ({
      link,
      target: document.querySelector(link.hash)
    }))
    .filter((item) => item.target);

  const updateTopNav = () => {
    if (!topNav) {
      return;
    }

    topNav.classList.toggle("is-scrolled", window.scrollY > 50);
  };

  const updateParallax = () => {
    if (heroSlides.length === 0 || motionQuery.matches) {
      return;
    }

    heroSlides.forEach((slide) => {
      slide.style.transform = `translateY(${window.scrollY * 0.4}px)`;
    });
  };

  const showHeroSlide = (index) => {
    heroSlides.forEach((slide, slideIndex) => {
      slide.classList.toggle("hero__slide--active", slideIndex === index);
    });
  };

  const updateDockState = () => {
    if (dockTargets.length === 0) {
      return;
    }

    const activeItem = dockTargets.reduce((current, item) => {
      const distance = Math.abs(item.target.getBoundingClientRect().top);
      return distance < current.distance ? { item, distance } : current;
    }, { item: dockTargets[0], distance: Number.POSITIVE_INFINITY }).item;

    dockLinks.forEach((link) => link.classList.toggle("dock__item--active", link === activeItem.link));
  };

  window.addEventListener("scroll", () => {
    updateTopNav();
    updateParallax();
    updateDockState();
  }, { passive: true });

  updateTopNav();
  updateParallax();
  updateDockState();

  if (heroSlides.length > 1 && !motionQuery.matches) {
    window.setInterval(() => {
      activeHeroSlide = (activeHeroSlide + 1) % heroSlides.length;
      showHeroSlide(activeHeroSlide);
    }, 5000);
  }

  if (themeToggle) {
    themeToggle.addEventListener("click", () => {
      document.body.classList.toggle("theme-dark");
    });
  }

  document.querySelectorAll(".top-menu__group").forEach((group) => {
    const trigger = group.querySelector(".top-menu__item");
    if (!trigger) {
      return;
    }

    group.addEventListener("mouseenter", () => {
      trigger.setAttribute("aria-expanded", "true");
    });

    group.addEventListener("mouseleave", () => {
      trigger.setAttribute("aria-expanded", "false");
    });

    group.addEventListener("focusin", () => {
      trigger.setAttribute("aria-expanded", "true");
    });

    group.addEventListener("focusout", (event) => {
      if (!group.contains(event.relatedTarget)) {
        trigger.setAttribute("aria-expanded", "false");
      }
    });
  });

  const adminModal = document.querySelector("[data-admin-modal]");
  const adminModalOpenControls = document.querySelectorAll("[data-admin-modal-open]");
  const adminModalCloseControls = document.querySelectorAll("[data-admin-modal-close]");
  const adminPasswordForm = document.querySelector("[data-admin-password-form]");
  const adminPasswordInput = document.querySelector("[data-admin-password-input]");
  const adminPasswordError = document.querySelector("[data-admin-password-error]");

  const closeAdminModal = () => {
    if (!adminModal) {
      return;
    }

    adminModal.hidden = true;
    document.body.classList.remove("is-modal-open");

    if (adminPasswordInput) {
      adminPasswordInput.value = "";
    }

    if (adminPasswordError) {
      adminPasswordError.hidden = true;
    }
  };

  if (adminModal && adminModalOpenControls.length > 0) {
    adminModalOpenControls.forEach((control) => control.addEventListener("click", () => {
      adminModal.hidden = false;
      document.body.classList.add("is-modal-open");
      requestAnimationFrame(() => adminPasswordInput?.focus());
    }));
  }

  adminModalCloseControls.forEach((control) => {
    control.addEventListener("click", closeAdminModal);
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && adminModal && !adminModal.hidden) {
      closeAdminModal();
    }
  });

  if (adminPasswordForm && adminPasswordInput) {
    adminPasswordForm.addEventListener("submit", (event) => {
      event.preventDefault();

      if (adminPasswordInput.value === "qwerty") {
        window.location.href = "/Home/Admin";
        return;
      }

      if (adminPasswordError) {
        adminPasswordError.hidden = false;
      }

      adminPasswordInput.select();
    });
  }

  const adminShell = document.querySelector(".admin-shell");
  const adminScrollKey = "nzo-admin-scroll-y";
  const saveAdminScrollPosition = () => {
    sessionStorage.setItem(adminScrollKey, String(window.scrollY));
  };

  if (adminShell) {
    if ("scrollRestoration" in history) {
      history.scrollRestoration = "manual";
    }

    const savedScrollPosition = Number(sessionStorage.getItem(adminScrollKey));
    if (Number.isFinite(savedScrollPosition) && savedScrollPosition > 0) {
      sessionStorage.removeItem(adminScrollKey);
      requestAnimationFrame(() => {
        window.scrollTo({ top: savedScrollPosition, behavior: "auto" });
      });
    }

    adminShell.querySelectorAll("form").forEach((form) => {
      form.addEventListener("submit", saveAdminScrollPosition);
    });

    adminShell.querySelectorAll(".finder-sidebar__item, .finder-sidebar__subitem").forEach((link) => {
      link.addEventListener("click", saveAdminScrollPosition);
    });
  }

  document.addEventListener("submit", (event) => {
    const form = event.target.closest("form[data-confirm]");
    if (form && !window.confirm(form.dataset.confirm)) {
      event.preventDefault();
    }
  });

  const showAdminMessage = (message) => {
    if (!adminShell || !message) {
      return;
    }

    let alert = adminShell.querySelector(".admin-alert");
    if (!alert) {
      alert = document.createElement("div");
      alert.className = "admin-alert";
      alert.setAttribute("role", "status");
      adminShell.querySelector(".admin-hero")?.after(alert);
    }

    alert.textContent = message;
  };

  const seriesActionPaths = new Set([
    "/Home/CreateFeaturedSeries",
    "/Home/UpdateFeaturedSeries",
    "/Home/AddFeaturedSeriesPhoto",
    "/Home/SetFeaturedSeriesCover",
    "/Home/RemoveFeaturedSeriesPhoto",
    "/Home/RemoveFeaturedSeries"
  ]);

  const getSeriesPanel = (seriesId) => document.querySelector(`[data-admin-series-panel="${CSS.escape(seriesId)}"]`);
  const getSeriesTab = (seriesId) => document.querySelector(`[data-admin-series-tab="${CSS.escape(seriesId)}"]`);
  const getTokenValue = (panel) => panel?.querySelector('input[name="__RequestVerificationToken"]')?.value || "";

  const appendHiddenInput = (form, name, value) => {
    const input = document.createElement("input");
    input.type = "hidden";
    input.name = name;
    input.value = value;
    form.append(input);
    return input;
  };

  const appendSeriesContext = (form, panel, seriesId, imageUrl = "") => {
    const token = getTokenValue(panel);
    if (token) {
      appendHiddenInput(form, "__RequestVerificationToken", token);
    }

    appendHiddenInput(form, "seriesId", seriesId);
    if (imageUrl) {
      appendHiddenInput(form, "imageUrl", imageUrl);
    }

    ["librarySource", "cloudinaryFolder"].forEach((name) => {
      const value = panel?.querySelector(`input[name="${name}"]`)?.value;
      if (value !== undefined) {
        appendHiddenInput(form, name, value);
      }
    });
  };

  const formatPhotoCount = (count) => `${count} photo${count === 1 ? "" : "s"}`;

  const normalizeSeriesPayload = (series) => ({
    id: series.id ?? series.Id,
    eyebrow: series.eyebrow ?? series.Eyebrow,
    title: series.title ?? series.Title,
    description: series.description ?? series.Description ?? "",
    orientation: series.orientation ?? series.Orientation,
    coverImageUrl: series.coverImageUrl ?? series.CoverImageUrl,
    photoUrls: series.photoUrls ?? series.PhotoUrls ?? []
  });

  const renderSeriesPhotoStrip = (panel, series) => {
    const photos = panel.querySelector(".featured-series-admin__photos");
    if (!photos) {
      return;
    }

    photos.replaceChildren();
    series.photoUrls.forEach((photoUrl) => {
      const figure = document.createElement("figure");
      const image = document.createElement("img");
      const caption = document.createElement("figcaption");
      image.src = photoUrl;
      image.alt = `${series.title} photo`;

      if (photoUrl === series.coverImageUrl) {
        const strong = document.createElement("strong");
        strong.textContent = "Cover";
        caption.append(strong);
      } else {
        const form = document.createElement("form");
        const button = document.createElement("button");
        form.method = "post";
        form.action = "/Home/SetFeaturedSeriesCover";
        appendSeriesContext(form, panel, series.id, photoUrl);
        button.type = "submit";
        button.textContent = "Cover";
        form.append(button);
        caption.append(form);
      }

      figure.append(image, caption);
      photos.append(figure);
    });
  };

  const renderSeriesCoverModal = (panel, series) => {
    const modal = panel.querySelector(`[data-series-cover-modal="${CSS.escape(series.id)}"]`);
    const grid = modal?.querySelector(".series-cover-modal__grid");
    if (!modal || !grid) {
      return;
    }

    const heading = modal.querySelector("h3");
    if (heading) {
      heading.textContent = series.title;
    }
    grid.replaceChildren();
    series.photoUrls.forEach((photoUrl) => {
      const form = document.createElement("form");
      const button = document.createElement("button");
      const image = document.createElement("img");
      const label = document.createElement("span");
      const isCover = photoUrl === series.coverImageUrl;

      form.method = "post";
      form.action = "/Home/SetFeaturedSeriesCover";
      appendSeriesContext(form, panel, series.id, photoUrl);
      button.className = `series-cover-modal__choice${isCover ? " series-cover-modal__choice--active" : ""}`;
      button.type = "submit";
      image.src = photoUrl;
      image.alt = `${series.title} cover option`;
      label.textContent = isCover ? "Current cover" : "Set cover";
      button.append(image, label);
      form.append(button);
      grid.append(form);
    });
  };

  const renderSeriesPhotosModal = (panel, series) => {
    const modal = panel.querySelector(`[data-series-photos-modal="${CSS.escape(series.id)}"]`);
    if (!modal) {
      return;
    }

    const heading = modal.querySelector("h3");
    if (heading) {
      heading.textContent = series.title;
    }
    let grid = modal.querySelector(".series-photos-modal__grid");
    let empty = modal.querySelector("[data-series-photos-empty]");
    const dialog = modal.querySelector(".series-photos-modal__dialog");
    if (!empty) {
      empty = document.createElement("p");
      empty.dataset.seriesPhotosEmpty = "";
      empty.textContent = "No photos in this series yet.";
      dialog?.append(empty);
    }
    if (!grid) {
      grid = document.createElement("div");
      grid.className = "series-photos-modal__grid";
      dialog?.append(grid);
    }

    grid.replaceChildren();
    grid.hidden = series.photoUrls.length === 0;
    if (empty) {
      empty.hidden = series.photoUrls.length > 0;
    }

    series.photoUrls.forEach((photoUrl) => {
      const figure = document.createElement("figure");
      const image = document.createElement("img");
      const caption = document.createElement("figcaption");
      const removeForm = document.createElement("form");
      const removeButton = document.createElement("button");

      image.src = photoUrl;
      image.alt = `${series.title} photo`;
      if (photoUrl === series.coverImageUrl) {
        const strong = document.createElement("strong");
        strong.textContent = "Cover";
        caption.append(strong);
      }

      removeForm.method = "post";
      removeForm.action = "/Home/RemoveFeaturedSeriesPhoto";
      removeForm.dataset.confirm = "Remove this photo from the series?";
      appendSeriesContext(removeForm, panel, series.id, photoUrl);
      removeButton.className = "series-photos-modal__remove";
      removeButton.type = "submit";
      removeButton.textContent = "Remove photo";
      removeForm.append(removeButton);
      caption.append(removeForm);
      figure.append(image, caption);
      grid.append(figure);
    });
  };

  const refreshSeriesPhotoPicker = (picker) => {
    const checkboxes = [...picker.querySelectorAll('input[name="imageUrl"]')];
    const addButton = picker.querySelector(".series-photos-modal__add-selected");
    if (!addButton) {
      return;
    }

    const selectedCount = checkboxes.filter((checkbox) => checkbox.checked).length;
    addButton.hidden = selectedCount === 0;
    addButton.textContent = selectedCount === 1 ? "Add 1 photo" : `Add ${selectedCount} photos`;

    checkboxes.forEach((checkbox) => {
      const status = checkbox.closest(".series-photos-modal__library-choice")?.querySelector("strong");
      if (status && !checkbox.disabled) {
        status.textContent = checkbox.checked ? "Selected" : "Select";
      }
    });
  };

  const updateSeriesPickerState = (panel, series) => {
    const picker = panel.querySelector("[data-series-photo-picker]");
    if (!picker) {
      return;
    }

    const photoUrls = new Set(series.photoUrls);
    picker.querySelectorAll('input[name="imageUrl"]').forEach((checkbox) => {
      const isAdded = photoUrls.has(checkbox.value);
      const choice = checkbox.closest(".series-photos-modal__library-choice");
      const status = choice?.querySelector("strong");
      checkbox.checked = false;
      checkbox.disabled = isAdded;
      choice?.classList.toggle("series-photos-modal__library-choice--added", isAdded);
      if (status) {
        status.textContent = isAdded ? "Added" : "Select";
      }
    });

    refreshSeriesPhotoPicker(picker);
  };

  const createField = (labelText, control) => {
    const label = document.createElement("label");
    const span = document.createElement("span");
    span.textContent = labelText;
    label.append(span, control);
    return label;
  };

  const createSeriesPanel = (series, sourceForm) => {
    const finder = document.querySelector("[data-admin-series-finder]");
    const list = finder?.querySelector(".featured-series-finder__list");
    const detail = finder?.querySelector(".featured-series-finder__detail");
    if (!finder || !list || !detail) {
      return null;
    }

    const token = sourceForm.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    const librarySource = sourceForm.querySelector('input[name="librarySource"]')?.value || "";
    const cloudinaryFolder = sourceForm.querySelector('input[name="cloudinaryFolder"]')?.value || "";
    const tab = document.createElement("button");
    const tabImage = document.createElement("img");
    const tabText = document.createElement("span");
    const tabTitle = document.createElement("strong");
    const tabMeta = document.createElement("small");
    tab.className = "featured-series-finder__item";
    tab.type = "button";
    tab.dataset.adminSeriesTab = series.id;
    tab.setAttribute("aria-pressed", "false");
    tabImage.src = series.coverImageUrl;
    tabImage.alt = "";
    tabTitle.textContent = series.title;
    tabMeta.textContent = `${series.eyebrow} · ${series.orientation} · ${formatPhotoCount(series.photoUrls.length)}`;
    tabText.append(tabTitle, tabMeta);
    tab.append(tabImage, tabText);
    tab.addEventListener("click", () => showSeriesPanel(series.id));
    list.append(tab);

    const panel = document.createElement("article");
    const cover = document.createElement("div");
    const coverImage = document.createElement("img");
    const coverBadge = document.createElement("span");
    const body = document.createElement("div");
    const editForm = document.createElement("form");
    const eyebrowInput = document.createElement("input");
    const titleInput = document.createElement("input");
    const orientationSelect = document.createElement("select");
    const descriptionTextarea = document.createElement("textarea");
    const saveButton = document.createElement("button");
    const tools = document.createElement("div");
    const coverTool = document.createElement("div");
    const coverToolLabel = document.createElement("span");
    const coverToolButton = document.createElement("button");
    const photosTool = document.createElement("div");
    const photosToolLabel = document.createElement("span");
    const photosToolButton = document.createElement("button");
    const removeForm = document.createElement("form");
    const removeButton = document.createElement("button");
    const photoStrip = document.createElement("div");
    const photosModal = document.createElement("div");
    const photosBackdrop = document.createElement("div");
    const photosDialog = document.createElement("section");
    const photosClose = document.createElement("button");
    const photosLabel = document.createElement("span");
    const photosTitle = document.createElement("h3");
    const photosForms = document.createElement("div");
    const picker = document.createElement("form");
    const pickerLabel = document.createElement("span");
    const pickerGrid = document.createElement("div");
    const addSelected = document.createElement("button");
    const photosEmpty = document.createElement("p");
    const photosGrid = document.createElement("div");
    const coverModal = document.createElement("div");
    const coverBackdrop = document.createElement("div");
    const coverDialog = document.createElement("section");
    const coverClose = document.createElement("button");
    const coverLabel = document.createElement("span");
    const coverTitle = document.createElement("h3");
    const coverGrid = document.createElement("div");

    panel.className = "featured-series-admin__card";
    panel.dataset.adminSeriesPanel = series.id;
    panel.hidden = true;
    cover.className = "featured-series-admin__cover";
    coverImage.src = series.coverImageUrl;
    coverImage.alt = `${series.title} cover`;
    coverBadge.textContent = series.orientation;
    cover.append(coverImage, coverBadge);
    body.className = "featured-series-admin__body";

    editForm.className = "featured-series-admin__edit";
    editForm.method = "post";
    editForm.action = "/Home/UpdateFeaturedSeries";
    if (token) {
      appendHiddenInput(editForm, "__RequestVerificationToken", token);
    }
    appendHiddenInput(editForm, "seriesId", series.id);
    appendHiddenInput(editForm, "librarySource", librarySource);
    appendHiddenInput(editForm, "cloudinaryFolder", cloudinaryFolder);
    eyebrowInput.name = "eyebrow";
    titleInput.name = "title";
    titleInput.required = true;
    ["landscape", "portrait"].forEach((orientation) => {
      const option = document.createElement("option");
      option.value = orientation;
      option.textContent = orientation === "landscape" ? "Landscape" : "Portrait";
      orientationSelect.append(option);
    });
    orientationSelect.name = "orientation";
    descriptionTextarea.name = "description";
    descriptionTextarea.rows = 3;
    saveButton.type = "submit";
    saveButton.textContent = "Save series";
    editForm.append(
      createField("Label", eyebrowInput),
      createField("Title", titleInput),
      createField("Orientation", orientationSelect),
      createField("Description", descriptionTextarea),
      saveButton);

    tools.className = "featured-series-admin__tools";
    coverTool.className = "featured-series-admin__tool-panel";
    coverToolLabel.textContent = "Cover image";
    coverToolButton.type = "button";
    coverToolButton.dataset.seriesCoverModalOpen = series.id;
    coverToolButton.textContent = "Choose cover";
    coverTool.append(coverToolLabel, coverToolButton);
    photosTool.className = "featured-series-admin__tool-panel";
    photosToolLabel.textContent = "Photos";
    photosToolButton.type = "button";
    photosToolButton.dataset.seriesPhotosModalOpen = series.id;
    photosToolButton.textContent = "Manage photos";
    photosTool.append(photosToolLabel, photosToolButton);
    removeForm.className = "featured-series-admin__remove-series";
    removeForm.method = "post";
    removeForm.action = "/Home/RemoveFeaturedSeries";
    removeForm.dataset.confirm = "Remove this entire featured series?";
    if (token) {
      appendHiddenInput(removeForm, "__RequestVerificationToken", token);
    }
    appendHiddenInput(removeForm, "seriesId", series.id);
    appendHiddenInput(removeForm, "librarySource", librarySource);
    appendHiddenInput(removeForm, "cloudinaryFolder", cloudinaryFolder);
    removeButton.type = "submit";
    removeButton.textContent = "Remove series";
    removeForm.append(removeButton);
    tools.append(coverTool, photosTool, removeForm);

    photoStrip.className = "featured-series-admin__photos";
    photoStrip.setAttribute("aria-label", `${series.title} photos`);

    photosModal.className = "series-photos-modal";
    photosModal.dataset.seriesPhotosModal = series.id;
    photosModal.hidden = true;
    photosBackdrop.className = "series-photos-modal__backdrop";
    photosBackdrop.dataset.seriesPhotosModalClose = "";
    photosDialog.className = "series-photos-modal__dialog";
    photosDialog.setAttribute("role", "dialog");
    photosDialog.setAttribute("aria-modal", "true");
    photosDialog.setAttribute("aria-labelledby", `series-photos-title-${series.id}`);
    photosClose.className = "series-photos-modal__close";
    photosClose.type = "button";
    photosClose.setAttribute("aria-label", "Close photo manager");
    photosClose.dataset.seriesPhotosModalClose = "";
    photosClose.innerHTML = '<span class="material-symbols-outlined" aria-hidden="true">close</span>';
    photosLabel.className = "label";
    photosLabel.textContent = "Photos";
    photosTitle.id = `series-photos-title-${series.id}`;
    photosTitle.textContent = series.title;
    photosForms.className = "series-photos-modal__forms";
    picker.className = "series-photos-modal__library";
    picker.method = "post";
    picker.action = "/Home/AddFeaturedSeriesPhoto";
    picker.dataset.seriesPhotoPicker = "";
    if (token) {
      appendHiddenInput(picker, "__RequestVerificationToken", token);
    }
    appendHiddenInput(picker, "seriesId", series.id);
    appendHiddenInput(picker, "librarySource", librarySource);
    appendHiddenInput(picker, "cloudinaryFolder", cloudinaryFolder);
    pickerLabel.textContent = "Add existing photo";
    pickerGrid.className = "series-photos-modal__library-grid";
    const sourcePicker = document.querySelector("[data-series-photo-picker]");
    sourcePicker?.querySelectorAll(".series-photos-modal__library-choice").forEach((choice) => {
      const clone = choice.cloneNode(true);
      const checkbox = clone.querySelector('input[name="imageUrl"]');
      const status = clone.querySelector("strong");
      if (checkbox) {
        const isAdded = series.photoUrls.includes(checkbox.value);
        checkbox.checked = false;
        checkbox.disabled = isAdded;
        clone.classList.toggle("series-photos-modal__library-choice--added", isAdded);
        if (status) {
          status.textContent = isAdded ? "Added" : "Select";
        }
        checkbox.addEventListener("change", () => refreshSeriesPhotoPicker(picker));
      }
      pickerGrid.append(clone);
    });
    addSelected.className = "series-photos-modal__add-selected";
    addSelected.type = "submit";
    addSelected.hidden = true;
    addSelected.textContent = "Add selected photos";
    picker.append(pickerLabel, pickerGrid, addSelected);
    photosForms.append(picker);
    photosEmpty.dataset.seriesPhotosEmpty = "";
    photosEmpty.textContent = "No photos in this series yet.";
    photosGrid.className = "series-photos-modal__grid";
    photosDialog.append(photosClose, photosLabel, photosTitle, photosForms, photosEmpty, photosGrid);
    photosModal.append(photosBackdrop, photosDialog);

    coverModal.className = "series-cover-modal";
    coverModal.dataset.seriesCoverModal = series.id;
    coverModal.hidden = true;
    coverBackdrop.className = "series-cover-modal__backdrop";
    coverBackdrop.dataset.seriesCoverModalClose = "";
    coverDialog.className = "series-cover-modal__dialog";
    coverDialog.setAttribute("role", "dialog");
    coverDialog.setAttribute("aria-modal", "true");
    coverDialog.setAttribute("aria-labelledby", `series-cover-title-${series.id}`);
    coverClose.className = "series-cover-modal__close";
    coverClose.type = "button";
    coverClose.setAttribute("aria-label", "Close cover picker");
    coverClose.dataset.seriesCoverModalClose = "";
    coverClose.innerHTML = '<span class="material-symbols-outlined" aria-hidden="true">close</span>';
    coverLabel.className = "label";
    coverLabel.textContent = "Cover Image";
    coverTitle.id = `series-cover-title-${series.id}`;
    coverTitle.textContent = series.title;
    coverGrid.className = "series-cover-modal__grid";
    coverDialog.append(coverClose, coverLabel, coverTitle, coverGrid);
    coverModal.append(coverBackdrop, coverDialog);

    body.append(editForm, tools, photoStrip, photosModal, coverModal);
    panel.append(cover, body);
    detail.append(panel);
    updateSeriesDom(series);
    showSeriesPanel(series.id);
    return panel;
  };

  const updateSeriesDom = (series) => {
    const panel = getSeriesPanel(series.id);
    if (!panel) {
      return;
    }

    const photoCount = series.photoUrls.length;
    const tab = getSeriesTab(series.id);
    tab?.querySelector("img")?.setAttribute("src", series.coverImageUrl);
    if (tab?.querySelector("strong")) {
      tab.querySelector("strong").textContent = series.title;
    }
    if (tab?.querySelector("small")) {
      tab.querySelector("small").textContent = `${series.eyebrow} · ${series.orientation} · ${formatPhotoCount(photoCount)}`;
    }

    const coverImage = panel.querySelector(".featured-series-admin__cover img");
    if (coverImage) {
      coverImage.src = series.coverImageUrl;
      coverImage.alt = `${series.title} cover`;
    }
    const coverBadge = panel.querySelector(".featured-series-admin__cover span");
    if (coverBadge) {
      coverBadge.textContent = series.orientation;
    }

    const eyebrowInput = panel.querySelector('input[name="eyebrow"]');
    const titleInput = panel.querySelector('input[name="title"]');
    const descriptionInput = panel.querySelector('textarea[name="description"]');
    const orientationInput = panel.querySelector('select[name="orientation"]');
    if (eyebrowInput) {
      eyebrowInput.value = series.eyebrow;
    }
    if (titleInput) {
      titleInput.value = series.title;
    }
    if (descriptionInput) {
      descriptionInput.value = series.description;
    }
    if (orientationInput) {
      orientationInput.value = series.orientation;
    }

    renderSeriesPhotoStrip(panel, series);
    renderSeriesCoverModal(panel, series);
    renderSeriesPhotosModal(panel, series);
    updateSeriesPickerState(panel, series);
  };

  const showSeriesPanel = (seriesId) => {
    document.querySelectorAll("[data-admin-series-finder]").forEach((finder) => {
      finder.querySelectorAll("[data-admin-series-tab]").forEach((tab) => {
        const isActive = tab.dataset.adminSeriesTab === seriesId;
        tab.classList.toggle("featured-series-finder__item--active", isActive);
        tab.setAttribute("aria-pressed", String(isActive));
      });

      finder.querySelectorAll("[data-admin-series-panel]").forEach((panel) => {
        panel.hidden = panel.dataset.adminSeriesPanel !== seriesId;
      });
    });
  };

  const removeSeriesDom = (seriesId) => {
    const tab = getSeriesTab(seriesId);
    const panel = getSeriesPanel(seriesId);
    const nextSeriesId = tab?.nextElementSibling?.dataset.adminSeriesTab || tab?.previousElementSibling?.dataset.adminSeriesTab || "";
    tab?.remove();
    panel?.remove();
    if (nextSeriesId) {
      showSeriesPanel(nextSeriesId);
    }
  };

  document.addEventListener("submit", async (event) => {
    const form = event.target.closest("form");
    if (!form || event.defaultPrevented) {
      return;
    }

    const actionPath = new URL(form.action, window.location.href).pathname;
    if (!seriesActionPaths.has(actionPath)) {
      return;
    }

    event.preventDefault();
    const submitButton = form.querySelector('button[type="submit"]');
    submitButton?.setAttribute("disabled", "true");

    try {
      const response = await fetch(form.action, {
        method: form.method || "post",
        body: new FormData(form),
        credentials: "same-origin",
        headers: {
          "Accept": "application/json",
          "X-Requested-With": "fetch"
        }
      });
      const payload = await response.json();
      if (!response.ok) {
        throw new Error(payload.message || payload.Message || "Series update failed.");
      }

      if (payload.removedSeriesId || payload.RemovedSeriesId) {
        removeSeriesDom(payload.removedSeriesId ?? payload.RemovedSeriesId);
      } else {
        const series = normalizeSeriesPayload(payload.series);
        if (!getSeriesPanel(series.id)) {
          createSeriesPanel(series, form);
          form.reset();
        } else {
          updateSeriesDom(series);
        }
      }
      showAdminMessage(payload.message ?? payload.Message);
    } catch (error) {
      showAdminMessage(error.message);
    } finally {
      submitButton?.removeAttribute("disabled");
    }
  });

  document.querySelectorAll("[data-series-gallery]").forEach((gallery) => {
    const grid = gallery.querySelector("[data-series-grid]");
    const sourceCards = [...gallery.querySelectorAll("[data-series-card]")];
    const viewButtons = [...gallery.querySelectorAll("[data-series-view]")];
    const previousButton = gallery.querySelector("[data-series-prev]");
    const nextButton = gallery.querySelector("[data-series-next]");
    const status = gallery.querySelector("[data-series-status]");
    const pageSize = Math.max(1, Number(gallery.dataset.seriesPageSize) || 5);
    const rowSize = Math.max(1, Math.floor(pageSize / 2));
    const landscapeCards = sourceCards.filter((card) => card.dataset.seriesOrientation === "landscape");
    const portraitCards = sourceCards.filter((card) => card.dataset.seriesOrientation === "portrait");
    const otherCards = sourceCards.filter((card) => !["landscape", "portrait"].includes(card.dataset.seriesOrientation));
    const cards = [];
    let currentPage = 0;

    for (let index = 0; index < Math.max(landscapeCards.length, portraitCards.length); index += rowSize) {
      cards.push(...landscapeCards.slice(index, index + rowSize));
      cards.push(...portraitCards.slice(index, index + rowSize));
    }

    cards.push(...otherCards);

    if (grid) {
      cards.forEach((card) => grid.append(card));
    }

    const renderSeriesPage = () => {
      const totalPages = Math.max(1, Math.ceil(cards.length / pageSize));
      currentPage = Math.min(Math.max(currentPage, 0), totalPages - 1);
      const start = currentPage * pageSize;
      const end = Math.min(start + pageSize, cards.length);

      cards.forEach((card, index) => {
        const isVisible = index >= start && index < end;
        card.hidden = !isVisible;

        if (isVisible) {
          card.classList.add("active");
        }
      });

      if (previousButton) {
        previousButton.disabled = currentPage === 0;
      }

      if (nextButton) {
        nextButton.disabled = currentPage >= totalPages - 1;
      }

      if (status) {
        status.textContent = cards.length === 0 ? "0 of 0" : `${start + 1}-${end} of ${cards.length}`;
      }
    };

    viewButtons.forEach((button) => {
      button.addEventListener("click", () => {
        const view = button.dataset.seriesView;
        if (!grid || !view) {
          return;
        }

        grid.classList.toggle("series-grid--grid", view === "grid");
        grid.classList.toggle("series-grid--masonry", view === "masonry");

        viewButtons.forEach((viewButton) => {
          const isActive = viewButton === button;
          viewButton.classList.toggle("series-view-toggle__button--active", isActive);
          viewButton.setAttribute("aria-pressed", String(isActive));
        });
      });
    });

    if (previousButton) {
      previousButton.addEventListener("click", () => {
        currentPage -= 1;
        renderSeriesPage();
      });
    }

    if (nextButton) {
      nextButton.addEventListener("click", () => {
        currentPage += 1;
        renderSeriesPage();
      });
    }

    renderSeriesPage();
  });

  document.querySelectorAll(".featured-series-admin").forEach((seriesAdmin) => {
    if (seriesAdmin.matches("[data-admin-series-finder]")) {
      return;
    }

    const cards = [...seriesAdmin.querySelectorAll(":scope > .featured-series-admin__card")];
    if (cards.length === 0) {
      return;
    }

    const list = document.createElement("aside");
    list.className = "featured-series-finder__list";
    list.setAttribute("aria-label", "Featured series");

    const detail = document.createElement("div");
    detail.className = "featured-series-finder__detail";

    cards.forEach((card, index) => {
      const seriesId = card.querySelector('input[name="seriesId"]')?.value || `series-${index}`;
      const title = card.querySelector('input[name="title"]')?.value || `Series ${index + 1}`;
      const eyebrow = card.querySelector('input[name="eyebrow"]')?.value || `Series ${index + 1}`;
      const orientation = card.querySelector('select[name="orientation"]')?.value || "portrait";
      const coverImage = card.querySelector(".featured-series-admin__cover img")?.getAttribute("src") || "";
      const photoCount = card.querySelectorAll(".featured-series-admin__photos figure").length;
      const isSelected = index === 0;
      const button = document.createElement("button");
      const image = document.createElement("img");
      const text = document.createElement("span");
      const strong = document.createElement("strong");
      const small = document.createElement("small");

      button.className = `featured-series-finder__item${isSelected ? " featured-series-finder__item--active" : ""}`;
      button.type = "button";
      button.dataset.adminSeriesTab = seriesId;
      button.setAttribute("aria-pressed", String(isSelected));
      image.src = coverImage;
      image.alt = "";
      strong.textContent = title;
      small.textContent = `${eyebrow} · ${orientation} · ${photoCount} photo${photoCount === 1 ? "" : "s"}`;
      text.append(strong, small);
      button.append(image, text);
      list.append(button);

      const coverForm = card.querySelector('.featured-series-admin__tools form[action*="SetFeaturedSeriesCover"]');
      if (coverForm) {
        const coverPanel = document.createElement("div");
        const coverButton = document.createElement("button");

        coverPanel.className = "featured-series-admin__tool-panel";
        coverPanel.innerHTML = "<span>Cover image</span>";
        coverButton.type = "button";
        coverButton.dataset.seriesCoverModalOpen = seriesId;
        coverButton.textContent = "Choose cover";
        coverPanel.append(coverButton);
        coverForm.replaceWith(coverPanel);
      }

      if (!card.querySelector(".featured-series-admin__remove-series")) {
        const tools = card.querySelector(".featured-series-admin__tools");
        const referenceForm = card.querySelector(".featured-series-admin__edit");
        const token = referenceForm?.querySelector('input[name="__RequestVerificationToken"]')?.cloneNode();
        const removeSeriesForm = document.createElement("form");
        const seriesInput = document.createElement("input");
        const librarySourceInput = card.querySelector('input[name="librarySource"]')?.cloneNode();
        const cloudinaryFolderInput = card.querySelector('input[name="cloudinaryFolder"]')?.cloneNode();
        const removeButton = document.createElement("button");

        removeSeriesForm.className = "featured-series-admin__remove-series";
        removeSeriesForm.method = "post";
        removeSeriesForm.action = "/Home/RemoveFeaturedSeries";
        removeSeriesForm.dataset.confirm = "Remove this entire featured series?";
        seriesInput.type = "hidden";
        seriesInput.name = "seriesId";
        seriesInput.value = seriesId;
        removeButton.type = "submit";
        removeButton.textContent = "Remove series";
        [token, seriesInput, librarySourceInput, cloudinaryFolderInput, removeButton]
          .filter(Boolean)
          .forEach((node) => removeSeriesForm.append(node));
        tools?.append(removeSeriesForm);
      }

      if (!card.querySelector("[data-series-cover-modal]")) {
        const modal = document.createElement("div");
        const dialog = document.createElement("section");
        const closeButton = document.createElement("button");
        const label = document.createElement("span");
        const heading = document.createElement("h3");
        const grid = document.createElement("div");

        modal.className = "series-cover-modal";
        modal.dataset.seriesCoverModal = seriesId;
        modal.hidden = true;
        modal.innerHTML = '<div class="series-cover-modal__backdrop" data-series-cover-modal-close></div>';
        dialog.className = "series-cover-modal__dialog";
        dialog.setAttribute("role", "dialog");
        dialog.setAttribute("aria-modal", "true");
        closeButton.className = "series-cover-modal__close";
        closeButton.type = "button";
        closeButton.setAttribute("aria-label", "Close cover picker");
        closeButton.dataset.seriesCoverModalClose = "";
        closeButton.innerHTML = '<span class="material-symbols-outlined" aria-hidden="true">close</span>';
        label.className = "label";
        label.textContent = "Cover Image";
        heading.textContent = title;
        grid.className = "series-cover-modal__grid";

        card.querySelectorAll(".featured-series-admin__photos figure img").forEach((photo) => {
          const photoUrl = photo.getAttribute("src");
          const referenceForm = card.querySelector(".featured-series-admin__edit");
          const token = referenceForm?.querySelector('input[name="__RequestVerificationToken"]')?.cloneNode();
          const form = document.createElement("form");
          const seriesInput = document.createElement("input");
          const imageInput = document.createElement("input");
          const librarySourceInput = card.querySelector('input[name="librarySource"]')?.cloneNode();
          const cloudinaryFolderInput = card.querySelector('input[name="cloudinaryFolder"]')?.cloneNode();
          const choiceButton = document.createElement("button");
          const choiceImage = document.createElement("img");
          const choiceLabel = document.createElement("span");
          const isCover = photoUrl === coverImage;

          form.method = "post";
          form.action = "/Home/SetFeaturedSeriesCover";
          seriesInput.type = "hidden";
          seriesInput.name = "seriesId";
          seriesInput.value = seriesId;
          imageInput.type = "hidden";
          imageInput.name = "imageUrl";
          imageInput.value = photoUrl;
          choiceButton.className = `series-cover-modal__choice${isCover ? " series-cover-modal__choice--active" : ""}`;
          choiceButton.type = "submit";
          choiceImage.src = photoUrl;
          choiceImage.alt = `${title} cover option`;
          choiceLabel.textContent = isCover ? "Current cover" : "Set cover";
          choiceButton.append(choiceImage, choiceLabel);
          [token, seriesInput, imageInput, librarySourceInput, cloudinaryFolderInput, choiceButton]
            .filter(Boolean)
            .forEach((node) => form.append(node));
          grid.append(form);
        });

        dialog.append(closeButton, label, heading, grid);
        modal.append(dialog);
        card.append(modal);
      }

      card.dataset.adminSeriesPanel = seriesId;
      card.hidden = !isSelected;
      detail.append(card);
    });

    seriesAdmin.classList.add("featured-series-finder");
    seriesAdmin.dataset.adminSeriesFinder = "";
    seriesAdmin.replaceChildren(list, detail);
  });

  document.querySelectorAll("[data-admin-series-finder]").forEach((finder) => {
    const tabs = [...finder.querySelectorAll("[data-admin-series-tab]")];
    const panels = [...finder.querySelectorAll("[data-admin-series-panel]")];

    const showSeriesPanel = (seriesId) => {
      tabs.forEach((tab) => {
        const isActive = tab.dataset.adminSeriesTab === seriesId;
        tab.classList.toggle("featured-series-finder__item--active", isActive);
        tab.setAttribute("aria-pressed", String(isActive));
      });

      panels.forEach((panel) => {
        panel.hidden = panel.dataset.adminSeriesPanel !== seriesId;
      });
    };

    tabs.forEach((tab) => {
      tab.addEventListener("click", () => {
        showSeriesPanel(tab.dataset.adminSeriesTab);
      });
    });

    const activeTab = tabs.find((tab) => tab.classList.contains("featured-series-finder__item--active")) ?? tabs[0];
    if (activeTab) {
      showSeriesPanel(activeTab.dataset.adminSeriesTab);
    }
  });

  document.querySelectorAll("[data-series-cover-modal-open]").forEach((control) => {
    const modal = document.querySelector(`[data-series-cover-modal="${control.dataset.seriesCoverModalOpen}"]`);
    if (!modal) {
      return;
    }

    control.addEventListener("click", () => {
      modal.hidden = false;
      document.body.classList.add("is-modal-open");
    });
  });

  document.querySelectorAll("[data-series-cover-modal]").forEach((modal) => {
    modal.querySelectorAll("[data-series-cover-modal-close]").forEach((control) => {
      control.addEventListener("click", () => {
        modal.hidden = true;
        document.body.classList.remove("is-modal-open");
      });
    });
  });

  document.querySelectorAll("[data-series-photos-modal-open]").forEach((control) => {
    const modal = document.querySelector(`[data-series-photos-modal="${control.dataset.seriesPhotosModalOpen}"]`);
    if (!modal) {
      return;
    }

    control.addEventListener("click", () => {
      modal.hidden = false;
    });
  });

  document.querySelectorAll("[data-series-photos-modal]").forEach((modal) => {
    modal.querySelectorAll("[data-series-photos-modal-close]").forEach((control) => {
      control.addEventListener("click", () => {
        modal.hidden = true;
      });
    });
  });

  document.addEventListener("click", (event) => {
    const coverOpen = event.target.closest("[data-series-cover-modal-open]");
    if (coverOpen) {
      const modal = document.querySelector(`[data-series-cover-modal="${CSS.escape(coverOpen.dataset.seriesCoverModalOpen)}"]`);
      if (modal) {
        modal.hidden = false;
        document.body.classList.add("is-modal-open");
      }
      return;
    }

    const coverClose = event.target.closest("[data-series-cover-modal-close]");
    if (coverClose) {
      const modal = coverClose.closest("[data-series-cover-modal]");
      if (modal) {
        modal.hidden = true;
        document.body.classList.remove("is-modal-open");
      }
      return;
    }

    const photosOpen = event.target.closest("[data-series-photos-modal-open]");
    if (photosOpen) {
      const modal = document.querySelector(`[data-series-photos-modal="${CSS.escape(photosOpen.dataset.seriesPhotosModalOpen)}"]`);
      if (modal) {
        modal.hidden = false;
      }
      return;
    }

    const photosClose = event.target.closest("[data-series-photos-modal-close]");
    if (photosClose) {
      const modal = photosClose.closest("[data-series-photos-modal]");
      if (modal) {
        modal.hidden = true;
      }
    }
  });

  document.querySelectorAll("[data-series-photo-picker]").forEach((picker) => {
    const checkboxes = [...picker.querySelectorAll('input[name="imageUrl"]')];

    checkboxes.forEach((checkbox) => {
      checkbox.addEventListener("change", () => refreshSeriesPhotoPicker(picker));
    });

    refreshSeriesPhotoPicker(picker);
  });

  const uploadInput = document.querySelector("[data-upload-input]");
  const uploadLabel = document.querySelector("[data-upload-label]");
  const uploadPreview = document.querySelector("[data-upload-preview]");
  const uploadPreviewGrid = document.querySelector("[data-upload-preview-grid]");
  const uploadClear = document.querySelector("[data-upload-clear]");
  let selectedUploadFiles = [];
  let uploadPreviewUrls = [];

  const clearUploadPreviewUrls = () => {
    uploadPreviewUrls.forEach((url) => URL.revokeObjectURL(url));
    uploadPreviewUrls = [];
  };

  const formatFileSize = (size) => {
    if (size < 1024 * 1024) {
      return `${Math.max(1, Math.round(size / 1024))} KB`;
    }

    return `${(size / (1024 * 1024)).toFixed(1)} MB`;
  };

  const resetUploadPreview = () => {
    clearUploadPreviewUrls();
    selectedUploadFiles = [];

    if (uploadPreviewGrid) {
      uploadPreviewGrid.replaceChildren();
    }

    if (uploadPreview) {
      uploadPreview.hidden = true;
    }

    if (uploadLabel) {
      uploadLabel.textContent = "Add images";
    }
  };

  const syncUploadInputFiles = () => {
    if (!uploadInput || typeof DataTransfer === "undefined") {
      return;
    }

    const dataTransfer = new DataTransfer();
    selectedUploadFiles.forEach((file) => dataTransfer.items.add(file));
    uploadInput.files = dataTransfer.files;
  };

  const renderUploadPreview = () => {
    clearUploadPreviewUrls();

    if (uploadPreviewGrid) {
      uploadPreviewGrid.replaceChildren();
    }

    if (selectedUploadFiles.length === 0) {
      if (uploadPreview) {
        uploadPreview.hidden = true;
      }

      if (uploadLabel) {
        uploadLabel.textContent = "Add images";
      }

      syncUploadInputFiles();
      return;
    }

    if (uploadLabel) {
      uploadLabel.textContent = `${selectedUploadFiles.length} image${selectedUploadFiles.length === 1 ? "" : "s"} selected`;
    }

    selectedUploadFiles.forEach((file, index) => {
      const imageUrl = URL.createObjectURL(file);
      uploadPreviewUrls.push(imageUrl);

      const card = document.createElement("article");
      card.className = "upload-preview-card";

      const image = document.createElement("img");
      image.src = imageUrl;
      image.alt = file.name;

      const body = document.createElement("div");
      body.className = "upload-preview-card__body";

      const name = document.createElement("span");
      name.textContent = file.name;

      const meta = document.createElement("strong");
      meta.textContent = index === 0 ? `${formatFileSize(file.size)} | first` : formatFileSize(file.size);

      const removeButton = document.createElement("button");
      removeButton.type = "button";
      removeButton.textContent = "Remove";
      removeButton.addEventListener("click", () => {
        selectedUploadFiles.splice(index, 1);
        renderUploadPreview();
      });

      body.append(name, meta, removeButton);
      card.append(image, body);
      uploadPreviewGrid.append(card);
    });

    if (uploadPreview) {
      uploadPreview.hidden = false;
    }

    syncUploadInputFiles();
  };

  if (uploadInput && uploadPreview && uploadPreviewGrid) {
    uploadInput.addEventListener("change", () => {
      const incomingFiles = [...uploadInput.files].filter((file) => file.type.startsWith("image/"));
      const existingFileKeys = new Set(selectedUploadFiles.map((file) => `${file.name}:${file.size}:${file.lastModified}`));
      const newFiles = incomingFiles.filter((file) => !existingFileKeys.has(`${file.name}:${file.size}:${file.lastModified}`));
      selectedUploadFiles = selectedUploadFiles.concat(newFiles);
      renderUploadPreview();
    });
  }

  if (uploadInput && uploadClear) {
    uploadClear.addEventListener("click", () => {
      uploadInput.value = "";
      resetUploadPreview();
    });
  }

  const revealItems = document.querySelectorAll(".reveal");
  if ("IntersectionObserver" in window && !motionQuery.matches) {
    const revealObserver = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add("active");
          revealObserver.unobserve(entry.target);
        }
      });
    }, {
      threshold: 0.15,
      rootMargin: "0px 0px -50px 0px"
    });

    revealItems.forEach((item) => revealObserver.observe(item));
  } else {
    revealItems.forEach((item) => item.classList.add("active"));
  }

  if (!motionQuery.matches) {
    document.querySelectorAll(".tilt-card").forEach((card) => {
      card.addEventListener("mousemove", (event) => {
        const rect = card.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;
        const centerX = rect.width / 2;
        const centerY = rect.height / 2;
        const rotateX = (y - centerY) / 25;
        const rotateY = (centerX - x) / 25;

        card.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) scale3d(1.02, 1.02, 1.02)`;
        card.style.setProperty("--x", `${(x / rect.width) * 100}%`);
        card.style.setProperty("--y", `${(y / rect.height) * 100}%`);
      });

      card.addEventListener("mouseleave", () => {
        card.style.transform = "perspective(1000px) rotateX(0deg) rotateY(0deg) scale3d(1, 1, 1)";
      });
    });
  }
});
