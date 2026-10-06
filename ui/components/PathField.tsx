import { useState } from 'react';
import { FolderOpen } from 'lucide-react';
import { applyPathInputChange, browseAndApplyPath, resolvePathDrop } from './pathFieldModel';

type Props = {
  label: string;
  value: string;
  kind: 'file' | 'directory';
  required?: boolean;
  placeholder?: string;
  onChange(value: string): void;
  onBrowse(): Promise<string | null>;
};

export function PathField({ label, value, kind, required, placeholder, onChange, onBrowse }: Props) {
  const [dragging, setDragging] = useState(false);
  const [error, setError] = useState('');
  const id = `path-field-${label.toLowerCase().replace(/[^a-z0-9]+/g, '-')}`;
  const handleDrop = async (event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setDragging(false);
    const result = await resolvePathDrop(event.dataTransfer.files, async (file) => {
      const bridge = window.automator;
      if (!bridge) return '';
      return bridge.resolveDroppedFile(file);
    });
    setError(result.error ?? '');
    if (result.path) onChange(result.path);
  };
  return <div className={`path-field${dragging ? ' path-field-dragging' : ''}`} onDragEnter={(event) => { event.preventDefault(); setDragging(true); }} onDragOver={(event) => event.preventDefault()} onDragLeave={(event) => { if (!event.currentTarget.contains(event.relatedTarget as Node | null)) setDragging(false); }} onDrop={(event) => void handleDrop(event)}>
    <label htmlFor={id}>{label}</label>
    <span className="script-path-row"><input id={id} required={required} value={value} placeholder={placeholder ?? (kind === 'file' ? 'Paste or drop a file path' : 'Paste or drop a folder path')} aria-describedby={error ? `${id}-error` : undefined} onChange={(event) => { applyPathInputChange(event.target.value, onChange); setError(''); }} />
      <button className="secondary-button" type="button" onClick={async () => { setError(''); try { await browseAndApplyPath(onBrowse, onChange); } catch (cause) { setError(cause instanceof Error ? cause.message : 'Could not open the picker.'); } }}><FolderOpen size={13} /> Browse</button></span>
    {error && <span className="path-field-error" id={`${id}-error`} role="alert">{error}</span>}
    <span className="path-field-hint">Paste a path or drop one {kind === 'file' ? 'file' : 'folder'} here.</span>
  </div>;
}
