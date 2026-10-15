import { useId } from 'react';
import { Tag } from 'lucide-react';
import { applyWorkTimeTagSuggestion, suggestWorkTimeTags } from './workTimeViewModel';

type WorkTimeTagsInputProps = {
  value: string;
  onChange: (value: string) => void;
  /** Previously used tags, most relevant first. */
  knownTags: readonly string[];
  ariaLabel: string;
  name?: string;
  disabled?: boolean;
  placeholder?: string;
  hint?: boolean;
};

/** Comma-separated tag field that offers tags used in earlier entries as clickable suggestions. */
export function WorkTimeTagsInput({ value, onChange, knownTags, ariaLabel, name, disabled, placeholder = 'planning, release, bug fix', hint = true }: WorkTimeTagsInputProps) {
  const suggestionsId = useId();
  const suggestions = disabled ? [] : suggestWorkTimeTags(knownTags, value);
  return <div className="work-time-tags-field">
    <div className="work-time-tags-input"><Tag size={13} />
      <input name={name} aria-label={ariaLabel} value={value} maxLength={500} placeholder={placeholder} disabled={disabled}
        aria-controls={suggestions.length ? suggestionsId : undefined} autoComplete="off" onChange={(event) => onChange(event.target.value)} />
      {hint && <small>Separate with commas</small>}
    </div>
    {suggestions.length > 0 && <div className="work-time-tag-suggestions" id={suggestionsId} role="group" aria-label={`Suggested tags for ${ariaLabel}`}>
      <span>Used before</span>
      {suggestions.map((tag) => <button type="button" key={tag} className="work-time-tag-suggestion" aria-label={`Add tag ${tag}`}
        onMouseDown={(event) => event.preventDefault()} onClick={() => onChange(applyWorkTimeTagSuggestion(value, tag))}>{tag}</button>)}
    </div>}
  </div>;
}
