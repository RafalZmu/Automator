# Workflows test coverage

This file documents Workflow profile, variables, step-input contracts, and Electron editor visibility. Test titles match their source tests. The Electron case prepares a workflow using a saved Script Runner fixture and checks editor actions; persistence and execution are not completed by this case.

Run the focused cases with:

    node --experimental-strip-types --test tests/workflows-contract.test.mjs

Run all contract tests with npm.cmd run test:contracts. Application workflow specifications are included in npm.cmd run test:dotnet.

## Workflow contracts — tests/workflows-contract.test.mjs

### workflow contracts accept typed JSON values from an earlier step

**Steps**

1. Create a workflow whose API step writes an earlier result and whose Script Runner step maps that result into its input.
2. Validate the workflow profile.

**Expected result:** An ordered workflow can map typed JSON output from an earlier step.

### workflow contracts accept saved variables with arrays and typed JSON values

**Steps**

1. Add array, boolean, number, and object values to the workflow variables.
2. Validate the workflow profile.

**Expected result:** Variables retain their JSON types and the workflow remains valid.

### workflow contracts treat missing variables as an empty object for legacy profiles

**Steps**

1. Validate a workflow created without a variables property.
2. Read the workflow profile through the compatibility reader.

**Expected result:** The older workflow remains valid and reads with an empty variables object.

### workflow contracts reject malformed variable objects and invalid variable keys

**Steps**

1. Set variables to an array instead of an object and validate.
2. Set a key containing a disallowed slash and validate again.

**Expected result:** Both invalid shapes are rejected with a variable-specific validation message.

### workflow variable editor preserves JSON values and rejects duplicate or malformed entries

**Steps**

1. Parse variable editor rows containing a JSON array and boolean.
2. Try duplicate normalized keys, malformed JSON, more than 64 variables, and a value over 48 KiB.

**Expected result:** Valid values preserve JSON types; duplicate, malformed, excessive, and oversized entries return validation errors.

### workflow contracts preserve an explicit JSON null literal

**Steps**

1. Set a step input to a literal JSON null and mark the literal as present.
2. Validate the profile.

**Expected result:** Explicit null is distinguishable from an absent literal and is accepted.

### workflow contracts reject forward output references

**Steps**

1. Make the first step read the output of a later step.
2. Validate the profile.

**Expected result:** The forward reference is rejected because outputs can only come from earlier steps.

### workflow contracts accept mappings from the variables root input

**Steps**

1. Map the workflow input root into a step using the reserved $input source and a JSON Pointer.
2. Validate the profile.

**Expected result:** A valid root-variable mapping is accepted.

### workflow contracts reject JSON pointers with empty path segments

**Steps**

1. Set a destination pointer with a trailing slash.
2. Set another destination pointer with an empty middle segment.
3. Validate each mapping.

**Expected result:** Pointers containing empty segments are rejected.

### workflow contracts reject conflicting destination pointers

**Steps**

1. Map one value to /config.
2. Map a second value to a nested /config/mode destination.
3. Validate the workflow profile.

**Expected result:** Overlapping destination mappings are rejected as conflicting.

### workflow contracts reject unsupported modules and malformed JSON pointers

**Steps**

1. Change a step to an unsupported module and give it a malformed pointer.
2. Restore a supported module but keep the malformed pointer.

**Expected result:** Unsupported module IDs and malformed pointers are rejected.

### workflow contracts reject duplicate step ids and invalid binding shapes

**Steps**

1. Duplicate a step ID and validate.
2. Restore a unique ID, then set a binding that mixes a literal with a source reference.

**Expected result:** Duplicate IDs and bindings that specify more than one source are rejected.

### workflow contracts respect the RPC size limit for saved profiles

**Steps**

1. Add a literal value larger than the supported 48 KiB profile limit.
2. Validate the workflow.

**Expected result:** The oversized saved profile is rejected.

## Electron happy-path visibility — tests/electron/tab-happy-path-visibility.test.cjs

### Workflows editor happy path keeps step and save buttons within visible bounds

**Steps**

1. Save an isolated Script Runner profile through the UI to supply a valid workflow step.
2. Open Workflows, check Create a workflow and New workflow, then open the editor.
3. Check Close, enter a name and key, use checked Add step, and select the saved script profile.
4. Check Add mapping, Remove step 1, and Save workflow against viewport and clipping ancestors.
5. Use checked Cancel and verify the editor closes.

**Expected result:** Required editor buttons are CSS-visible, enabled, fully inside viewport and ancestor content bounds, and uncovered before use. User wheel scrolling may reveal controls before assertion; click auto-scrolling cannot satisfy it. A valid one-step draft can be prepared and canceled. This case does not claim save, saved-row Edit/Run, execution, mapping editing, or step-reordering coverage.

Run after npm.cmd run build:desktop with node --experimental-strip-types --test --test-concurrency=1 tests/electron/tab-happy-path-visibility.test.cjs.

**Observed persistence limitation:** During fixture development against the rebuilt desktop host, submitting this valid one-step draft through Save workflow returned "The module action request is invalid." and left the editor open. The saved Script Runner profile was present in the selector, and UI validation enabled Save. This transport failure prevents a supported save/edit/run fixture in the current UI. The visibility case therefore stops at the enabled Save button; the defect is reported separately and product code is unchanged.

## Additional backend coverage

Workflow execution ordering, input mapping, stop-on-failure behavior, and persisted history have Application specification coverage in tests/Automator.Workflows.Specs. Run it with npm.cmd run test:dotnet.
